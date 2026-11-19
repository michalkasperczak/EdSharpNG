using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace EdSharp.Pretranslation128
{
    public enum SnippetArchiveConflictPolicy { Skip, KeepBoth, ReplaceWithBackup }

    public sealed class SnippetArchiveLimits
    {
        public int MaxEntries { get; set; }
        public int MaxFiles { get; set; }
        public long MaxSingleFileBytes { get; set; }
        public long MaxTotalUncompressedBytes { get; set; }
        public SnippetArchiveLimits() { MaxEntries = 10000; MaxFiles = 10000; MaxSingleFileBytes = 64L * 1024L * 1024L; MaxTotalUncompressedBytes = 256L * 1024L * 1024L; }
        internal SnippetArchiveLimits Copy() { return new SnippetArchiveLimits { MaxEntries = MaxEntries, MaxFiles = MaxFiles, MaxSingleFileBytes = MaxSingleFileBytes, MaxTotalUncompressedBytes = MaxTotalUncompressedBytes }; }
    }

    public sealed class SnippetArchiveOptions
    {
        public SnippetArchiveLimits Limits { get; set; }
        // For ReplaceWithBackup this is a parent OUTSIDE the library. A unique child is reported in Result.
        public string BackupDirectory { get; set; }
        // Test seam only; production callers leave it null.
        public IArchiveFaultInjector FaultInjector { get; set; }
    }

    public interface IArchiveFaultInjector { void BeforeStageFile(string archiveEntryName); }

    public sealed class SnippetArchiveChange
    {
        public string RelativePath { get; internal set; }
        public string DestinationPath { get; internal set; }
        public string BackupPath { get; internal set; }
        public string Action { get; internal set; }
    }

    public sealed class SnippetArchiveResult
    {
        private readonly List<SnippetArchiveChange> changes = new List<SnippetArchiveChange>();
        private readonly List<string> skipped = new List<string>();
        private readonly List<string> rollbackIncomplete = new List<string>();
        public bool Succeeded { get; internal set; }
        public bool Cancelled { get; internal set; }
        public string Error { get; internal set; }
        public string BackupDirectory { get; internal set; }
        public IList<SnippetArchiveChange> Changes { get { return changes.AsReadOnly(); } }
        public IList<string> SkippedPaths { get { return skipped.AsReadOnly(); } }
        public IList<string> RollbackIncompletePaths { get { return rollbackIncomplete.AsReadOnly(); } }
        internal void AddChange(SnippetArchiveChange change) { changes.Add(change); }
        internal void AddSkipped(string path) { skipped.Add(path); }
        internal void AddRollbackIncomplete(string path) { rollbackIncomplete.Add(path); }
    }

    public sealed class SnippetArchive
    {
        public SnippetArchiveResult Export(string libraryFolder, string archiveZip, SnippetArchiveLimits limits, Func<bool> cancelRequested)
        {
            SnippetArchiveResult result = new SnippetArchiveResult();
            string temporary = null;
            try
            {
                ThrowIfCancelled(cancelRequested);
                string library = ExistingSafeDirectory(libraryFolder, "Biblioteka");
                string destination = FullPath(archiveZip, "Archiwum");
                EnsureSafeExistingParents(Path.GetDirectoryName(destination));
                string destinationDirectory = Path.GetDirectoryName(destination);
                if (!Directory.Exists(destinationDirectory)) throw new IOException(JezykInterfejsu.Wybierz("The archive destination folder does not exist.", "Katalog docelowy archiwum nie istnieje."));
                List<string> directories = new List<string>();
                List<string> files = new List<string>();
                CollectExportTree(library, library, destination, directories, files, cancelRequested);
                directories.Sort(StringComparer.Ordinal); files.Sort(StringComparer.Ordinal);
                temporary = Path.Combine(destinationDirectory, ".snippet-archive-" + Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (ZipArchive zip = new ZipArchive(output, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    foreach (string directory in directories)
                    {
                        ThrowIfCancelled(cancelRequested);
                        string name = Relative(library, directory).Replace(Path.DirectorySeparatorChar, '/') + "/";
                        zip.CreateEntry(name, CompressionLevel.Optimal);
                    }
                    foreach (string file in files)
                    {
                        ThrowIfCancelled(cancelRequested);
                        string name = Relative(library, file).Replace(Path.DirectorySeparatorChar, '/');
                        ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                        using (Stream input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (Stream entryStream = entry.Open()) CopyWithCancellation(input, entryStream, cancelRequested, null, null);
                    }
                }
                ThrowIfCancelled(cancelRequested);
                if (File.Exists(destination)) File.Replace(temporary, destination, null, true);
                else File.Move(temporary, destination);
                temporary = null;
                result.Succeeded = true;
            }
            catch (OperationCanceledException) { result.Cancelled = true; result.Error = JezykInterfejsu.Wybierz("The operation was cancelled.", "Operacja anulowana."); }
            catch (Exception ex) { result.Error = ex.Message; }
            finally { DeleteQuietly(temporary); }
            return result;
        }

        public SnippetArchiveResult Import(string archiveZip, string libraryFolder, SnippetArchiveConflictPolicy conflictPolicy, SnippetArchiveOptions options, Func<bool> cancelRequested)
        {
            SnippetArchiveResult result = new SnippetArchiveResult();
            string staging = null;
            Mutex mutex = null;
            bool mutexHeld = false;
            List<PlannedWrite> writes = null;
            try
            {
                ThrowIfCancelled(cancelRequested);
                string archive = ExistingFile(archiveZip, "Archiwum");
                string library = ExistingSafeDirectory(libraryFolder, "Biblioteka");
                SnippetArchiveLimits limits = GetLimits(options);
                List<ArchiveFile> archiveFiles;
                List<string> archiveDirectories;
                staging = Path.Combine(Path.GetTempPath(), "EdSharpSnippetArchive-stage-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(staging);
                long actualTotal = 0;
                // Entries belong to ZipArchive; retain it until every byte is staged and checked.
                using (ZipArchive zip = ZipFile.OpenRead(archive))
                {
                    archiveFiles = ValidateArchive(zip, limits, cancelRequested, out archiveDirectories);
                    foreach (ArchiveFile archiveFile in archiveFiles)
                    {
                        ThrowIfCancelled(cancelRequested);
                        if (options != null && options.FaultInjector != null) options.FaultInjector.BeforeStageFile(archiveFile.RelativePath);
                        string staged = SafeCombine(staging, archiveFile.RelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(staged));
                        using (Stream input = archiveFile.Entry.Open())
                        using (FileStream output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            long bytes = CopyWithCancellation(input, output, cancelRequested, limits, delegate(long count) { actualTotal += count; if (actualTotal > limits.MaxTotalUncompressedBytes) throw new InvalidDataException(JezykInterfejsu.Wybierz("The total extracted-byte limit was exceeded.", "Przekroczono limit wszystkich rozpakowanych bajtów.")); });
                            if (bytes > limits.MaxSingleFileBytes) throw new InvalidDataException(JezykInterfejsu.Wybierz("The single-file limit was exceeded.", "Przekroczono limit pojedynczego pliku."));
                        }
                        archiveFile.StagedPath = staged;
                    }
                }
                ThrowIfCancelled(cancelRequested);
                mutex = new Mutex(false, "Global\\EdSharpSnippetArchive-" + ShortHash(library));
                try { mutexHeld = mutex.WaitOne(); } catch (AbandonedMutexException) { mutexHeld = true; }
                writes = PlanWrites(library, archiveFiles, archiveDirectories, conflictPolicy, result);
                PrepareReplacementBackups(library, writes, options, result);
                CommitDirectories(library, archiveDirectories, result, cancelRequested);
                // Every replacement has a verified backup before any active file is changed.
                foreach (PlannedWrite write in writes) CommitWrite(write, result, cancelRequested);
                result.Succeeded = true;
            }
            catch (OperationCanceledException)
            {
                RollbackCommittedReplacements(writes, result);
                result.Cancelled = true; result.Error = RollbackError(JezykInterfejsu.Wybierz("The operation was cancelled.", "Operacja anulowana."), result);
            }
            catch (Exception ex)
            {
                RollbackCommittedReplacements(writes, result);
                result.Error = RollbackError(ex.Message, result);
            }
            finally
            {
                if (mutexHeld) mutex.ReleaseMutex();
                if (mutex != null) mutex.Dispose();
                DeleteDirectoryQuietly(staging);
            }
            return result;
        }

        private static SnippetArchiveLimits GetLimits(SnippetArchiveOptions options)
        {
            SnippetArchiveLimits l = options != null && options.Limits != null ? options.Limits.Copy() : new SnippetArchiveLimits();
            if (l.MaxEntries < 1 || l.MaxFiles < 1 || l.MaxSingleFileBytes < 1 || l.MaxTotalUncompressedBytes < 1) throw new ArgumentOutOfRangeException("limits", JezykInterfejsu.Wybierz("Limits must be positive.", "Limity muszą być dodatnie."));
            return l;
        }

        private static List<ArchiveFile> ValidateArchive(ZipArchive zip, SnippetArchiveLimits limits, Func<bool> cancelRequested, out List<string> directories)
        {
            directories = new List<string>();
            if (zip.Entries.Count > limits.MaxEntries) throw new InvalidDataException(JezykInterfejsu.Wybierz("The ZIP entry limit was exceeded.", "Przekroczono limit wpisów ZIP."));
            Dictionary<string, bool> types = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> explicitEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<ArchiveFile> files = new List<ArchiveFile>();
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                ThrowIfCancelled(cancelRequested);
                if (IsSymlinkOrReparse(entry)) throw new InvalidDataException(JezykInterfejsu.Wybierz("The ZIP contains a link or reparse point: ", "ZIP zawiera dowiązanie lub punkt ponownej analizy: ") + entry.FullName);
                bool directory = entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal);
                string relative = ValidateRelativeName(entry.FullName, directory);
                if (!explicitEntries.Add(relative)) throw new InvalidDataException(JezykInterfejsu.Wybierz("Duplicate ZIP name (also case-insensitive): ", "Zduplikowana nazwa ZIP (także bez rozróżniania wielkości liter): ") + entry.FullName);
                AddArchivePath(types, relative, directory);
                if (!directory)
                {
                    if (files.Count >= limits.MaxFiles) throw new InvalidDataException(JezykInterfejsu.Wybierz("The ZIP file-count limit was exceeded.", "Przekroczono limit plików ZIP."));
                    if (entry.Length > limits.MaxSingleFileBytes) throw new InvalidDataException(JezykInterfejsu.Wybierz("The declared file size exceeds the limit.", "Deklarowany rozmiar pliku przekracza limit."));
                    files.Add(new ArchiveFile { Entry = entry, RelativePath = relative });
                }
                else directories.Add(relative);
            }
            files.Sort(delegate(ArchiveFile a, ArchiveFile b) { return StringComparer.Ordinal.Compare(a.RelativePath, b.RelativePath); });
            directories.Sort(StringComparer.Ordinal);
            return files;
        }

        private static void AddArchivePath(Dictionary<string, bool> types, string relative, bool isDirectory)
        {
            string[] parts = relative.Split(new[] { '\\' }, StringSplitOptions.None);
            StringBuilder path = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) path.Append('\\'); path.Append(parts[i]);
                string current = path.ToString(); bool last = i == parts.Length - 1; bool wantedDirectory = last ? isDirectory : true;
                bool existing;
                if (types.TryGetValue(current, out existing))
                {
                    if (existing != wantedDirectory) throw new InvalidDataException(JezykInterfejsu.Wybierz("File/folder conflict in the ZIP: ", "Konflikt pliku i katalogu w ZIP: ") + current);
                }
                else types.Add(current, wantedDirectory);
            }
        }

        private static List<PlannedWrite> PlanWrites(string library, List<ArchiveFile> files, List<string> directories, SnippetArchiveConflictPolicy policy, SnippetArchiveResult result)
        {
            List<PlannedWrite> writes = new List<PlannedWrite>();
            HashSet<string> reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // KeepBoth names must not occupy a later original ZIP path or a required ZIP directory.
            foreach (ArchiveFile file in files) ReserveArchiveDestinationAndParents(library, file.RelativePath, reserved);
            foreach (string directory in directories) ReserveArchiveDestinationAndParents(library, directory, reserved);
            foreach (ArchiveFile file in files)
            {
                string destination = SafeCombine(library, file.RelativePath);
                EnsureSafeExistingParents(Path.GetDirectoryName(destination));
                if (Directory.Exists(destination) || File.Exists(destination)) EnsureNotReparse(destination);
                if (Directory.Exists(destination)) throw new IOException(JezykInterfejsu.Wybierz("File/folder conflict in the library: ", "Konflikt pliku i katalogu w bibliotece: ") + file.RelativePath);
                if (File.Exists(destination))
                {
                    if (policy == SnippetArchiveConflictPolicy.Skip) { result.AddSkipped(file.RelativePath); continue; }
                    if (policy == SnippetArchiveConflictPolicy.KeepBoth) destination = NextFreeName(library, file.RelativePath, reserved);
                    else if (policy != SnippetArchiveConflictPolicy.ReplaceWithBackup) throw new ArgumentOutOfRangeException("policy");
                }
                PlannedWrite write = new PlannedWrite { File = file, Destination = destination, Replace = File.Exists(destination) };
                if (write.Replace) write.OriginalHash = HashFile(destination);
                writes.Add(write);
                reserved.Add(destination);
            }
            return writes;
        }

        private static void ReserveArchiveDestinationAndParents(string library, string relative, HashSet<string> reserved)
        {
            string current = relative;
            while (!String.IsNullOrEmpty(current))
            {
                reserved.Add(SafeCombine(library, current));
                current = Path.GetDirectoryName(current);
            }
        }

        private static void PrepareReplacementBackups(string library, List<PlannedWrite> writes, SnippetArchiveOptions options, SnippetArchiveResult result)
        {
            bool needsBackup = false; foreach (PlannedWrite w in writes) if (w.Replace) { needsBackup = true; break; }
            if (!needsBackup) return;
            string baseDirectory = options == null ? null : options.BackupDirectory;
            if (String.IsNullOrWhiteSpace(baseDirectory)) baseDirectory = Path.Combine(Path.GetDirectoryName(library), "EdSharpSnippetArchiveBackups");
            baseDirectory = FullPath(baseDirectory, "Katalog kopii");
            if (IsChildOf(library, baseDirectory) || String.Equals(library, baseDirectory, StringComparison.OrdinalIgnoreCase)) throw new IOException(JezykInterfejsu.Wybierz("Replacement backups must be outside the library.", "Kopie zastąpień muszą leżeć poza biblioteką."));
            EnsureSafeExistingParents(Path.GetDirectoryName(baseDirectory));
            Directory.CreateDirectory(baseDirectory);
            string backupRoot = Path.Combine(baseDirectory, "snippet-archive-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backupRoot);
            result.BackupDirectory = backupRoot;
            foreach (PlannedWrite write in writes)
            {
                if (!write.Replace) continue;
                if (!String.Equals(HashFile(write.Destination), write.OriginalHash, StringComparison.Ordinal)) throw new IOException(JezykInterfejsu.Wybierz("The file changed externally before replacement: ", "Plik zmienił się zewnętrznie przed zastąpieniem: ") + write.File.RelativePath);
                string backup = SafeCombine(backupRoot, write.File.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(write.Destination, backup, false);
                if (!String.Equals(HashFile(backup), write.OriginalHash, StringComparison.Ordinal)) throw new IOException(JezykInterfejsu.Wybierz("The backup of the old file could not be verified: ", "Nie zweryfikowano kopii starego pliku: ") + write.File.RelativePath);
                write.Backup = backup;
            }
        }

        private static void CommitDirectories(string library, List<string> directories, SnippetArchiveResult result, Func<bool> cancelRequested)
        {
            foreach (string relative in directories)
            {
                ThrowIfCancelled(cancelRequested);
                string destination = SafeCombine(library, relative);
                EnsureSafeExistingParents(Path.GetDirectoryName(destination));
                if (Directory.Exists(destination)) EnsureNotReparse(destination);
                if (File.Exists(destination)) throw new IOException(JezykInterfejsu.Wybierz("File/folder conflict in the library: ", "Konflikt pliku i katalogu w bibliotece: ") + relative);
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                    result.AddChange(new SnippetArchiveChange { RelativePath = relative, DestinationPath = destination, Action = "added-directory" });
                }
            }
        }

        private static void CommitWrite(PlannedWrite write, SnippetArchiveResult result, Func<bool> cancelRequested)
        {
            ThrowIfCancelled(cancelRequested);
            Directory.CreateDirectory(Path.GetDirectoryName(write.Destination));
            string temporary = write.Destination + ".snippet-import-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(write.File.StagedPath, temporary, false);
                if (File.Exists(write.Destination)) EnsureNotReparse(write.Destination);
                if (write.Replace)
                {
                    if (!String.Equals(HashFile(write.Destination), write.OriginalHash, StringComparison.Ordinal)) throw new IOException(JezykInterfejsu.Wybierz("The file changed externally before replacement: ", "Plik zmienił się zewnętrznie przed zastąpieniem: ") + write.File.RelativePath);
                    write.CommittedHash = HashFile(write.File.StagedPath);
                    File.Replace(temporary, write.Destination, null, true);
                    write.Committed = true;
                    if (!String.Equals(HashFile(write.Destination), write.CommittedHash, StringComparison.Ordinal)) throw new IOException(JezykInterfejsu.Wybierz("The new file bytes could not be verified: ", "Nie zweryfikowano nowych bajtów: ") + write.File.RelativePath);
                }
                else File.Move(temporary, write.Destination);
                write.Change = new SnippetArchiveChange { RelativePath = write.File.RelativePath, DestinationPath = write.Destination, BackupPath = write.Backup, Action = write.Replace ? "replaced" : "added" };
                result.AddChange(write.Change);
            }
            finally { DeleteQuietly(temporary); }
        }

        private static void RollbackCommittedReplacements(List<PlannedWrite> writes, SnippetArchiveResult result)
        {
            if (writes == null) return;
            for (int i = writes.Count - 1; i >= 0; i--)
            {
                PlannedWrite write = writes[i];
                if (!write.Committed) continue;
                string temporary = null;
                try
                {
                    if (String.IsNullOrEmpty(write.Backup) || !File.Exists(write.Destination) || !String.Equals(HashFile(write.Destination), write.CommittedHash, StringComparison.Ordinal) || !String.Equals(HashFile(write.Backup), write.OriginalHash, StringComparison.Ordinal))
                    {
                        result.AddRollbackIncomplete(write.Destination);
                        continue;
                    }
                    temporary = write.Destination + ".snippet-rollback-" + Guid.NewGuid().ToString("N") + ".tmp";
                    File.Copy(write.Backup, temporary, false);
                    File.Replace(temporary, write.Destination, null, true);
                    if (!String.Equals(HashFile(write.Destination), write.OriginalHash, StringComparison.Ordinal))
                    {
                        result.AddRollbackIncomplete(write.Destination);
                        continue;
                    }
                    if (write.Change != null) write.Change.Action = "replaced-rolled-back";
                }
                catch { result.AddRollbackIncomplete(write.Destination); }
                finally { DeleteQuietly(temporary); }
            }
        }

        private static string RollbackError(string error, SnippetArchiveResult result)
        {
            if (result.RollbackIncompletePaths.Count == 0) return error;
            return error + " Niepełne wycofanie: " + String.Join(", ", result.RollbackIncompletePaths) + ". Kopie: " + (result.BackupDirectory ?? "brak");
        }

        private static string NextFreeName(string library, string relative, HashSet<string> reserved)
        {
            string directory = Path.GetDirectoryName(relative); string leaf = Path.GetFileNameWithoutExtension(relative); string extension = Path.GetExtension(relative);
            for (int n = 1; n < 100000; n++)
            {
                string candidateRelative = Path.Combine(directory ?? String.Empty, leaf + " (import " + n + ")" + extension);
                string candidate = SafeCombine(library, candidateRelative);
                if (!reserved.Contains(candidate) && !File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
            }
            throw new IOException(JezykInterfejsu.Wybierz("No free name preserving both versions was found.", "Nie znaleziono wolnej nazwy zachowującej obie wersje."));
        }

        private static void CollectExportTree(string root, string current, string outputZip, List<string> directories, List<string> files, Func<bool> cancelRequested)
        {
            ThrowIfCancelled(cancelRequested); EnsureNotReparse(current);
            foreach (string directory in Directory.GetDirectories(current)) { EnsureNotReparse(directory); directories.Add(directory); CollectExportTree(root, directory, outputZip, directories, files, cancelRequested); }
            foreach (string file in Directory.GetFiles(current)) { ThrowIfCancelled(cancelRequested); EnsureNotReparse(file); if (!String.Equals(FullPath(file, "plik"), outputZip, StringComparison.OrdinalIgnoreCase)) files.Add(file); }
        }

        private static string ValidateRelativeName(string name, bool directory)
        {
            if (String.IsNullOrEmpty(name)) throw new InvalidDataException(JezykInterfejsu.Wybierz("Empty ZIP name.", "Pusta nazwa ZIP."));
            string value = name.Replace('/', '\\'); if (directory) value = value.TrimEnd('\\');
            if (String.IsNullOrEmpty(value) || Path.IsPathRooted(value) || value.StartsWith("\\\\", StringComparison.Ordinal) || value.IndexOf(':') >= 0 || value.IndexOf('\0') >= 0) throw new InvalidDataException(JezykInterfejsu.Wybierz("Disallowed ZIP path: ", "Niedozwolona ścieżka ZIP: ") + name);
            string[] parts = value.Split(new[] { '\\' }, StringSplitOptions.None);
            foreach (string part in parts)
            {
                if (String.IsNullOrEmpty(part) || part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal) || IsReservedWindowsName(part)) throw new InvalidDataException(JezykInterfejsu.Wybierz("Disallowed ZIP name element: ", "Niedozwolony element nazwy ZIP: ") + name);
            }
            return String.Join("\\", parts);
        }

        private static bool IsReservedWindowsName(string segment)
        {
            string baseName = segment.Split('.')[0].ToUpperInvariant();
            if (baseName == "CON" || baseName == "PRN" || baseName == "AUX" || baseName == "NUL") return true;
            if (baseName.Length == 4 && (baseName.StartsWith("COM", StringComparison.Ordinal) || baseName.StartsWith("LPT", StringComparison.Ordinal)) && baseName[3] >= '1' && baseName[3] <= '9') return true;
            return false;
        }

        private static bool IsSymlinkOrReparse(ZipArchiveEntry entry)
        {
            int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
            return unixType == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0;
        }

        private static long CopyWithCancellation(Stream input, Stream output, Func<bool> cancelRequested, SnippetArchiveLimits limits, Action<long> increment)
        {
            byte[] buffer = new byte[81920]; long total = 0; int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ThrowIfCancelled(cancelRequested); total += read;
                if (limits != null && total > limits.MaxSingleFileBytes) throw new InvalidDataException(JezykInterfejsu.Wybierz("The single-file limit was exceeded.", "Przekroczono limit pojedynczego pliku."));
                if (increment != null) increment(read); output.Write(buffer, 0, read);
            }
            return total;
        }

        private static void ThrowIfCancelled(Func<bool> cancelRequested) { if (cancelRequested != null && cancelRequested()) throw new OperationCanceledException(); }
        private static string ExistingSafeDirectory(string path, string label) { string full = FullPath(path, label); if (!Directory.Exists(full)) throw new DirectoryNotFoundException(label + " nie istnieje."); EnsureSafeExistingParents(full); return full; }
        private static string ExistingFile(string path, string label) { string full = FullPath(path, label); if (!File.Exists(full)) throw new FileNotFoundException(label + " nie istnieje.", full); EnsureSafeExistingParents(Path.GetDirectoryName(full)); EnsureNotReparse(full); return full; }
        private static string FullPath(string path, string label) { if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(label + " jest puste."); return Path.GetFullPath(path); }
        private static void EnsureNotReparse(string path) { FileAttributes attributes = File.GetAttributes(path); if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException(JezykInterfejsu.Wybierz("A reparse point is not allowed: ", "Punkt ponownej analizy jest niedozwolony: ") + path); }
        private static void EnsureSafeExistingParents(string path)
        {
            string current = path;
            while (!String.IsNullOrEmpty(current) && !Directory.Exists(current)) { if (File.Exists(current)) throw new IOException(JezykInterfejsu.Wybierz("A file exists where a folder is expected: ", "Plik jest oczekiwanym katalogiem: ") + current); current = Path.GetDirectoryName(current); }
            if (!String.IsNullOrEmpty(current)) { EnsureNotReparse(current); string parent = Path.GetDirectoryName(current); while (!String.IsNullOrEmpty(parent)) { EnsureNotReparse(parent); parent = Path.GetDirectoryName(parent); } }
        }
        private static string SafeCombine(string root, string relative)
        {
            string combined = Path.GetFullPath(Path.Combine(root, relative));
            if (!IsChildOf(root, combined)) throw new InvalidDataException(JezykInterfejsu.Wybierz("The path escapes the destination folder.", "Ścieżka wychodzi poza katalog docelowy.")); return combined;
        }
        private static bool IsChildOf(string root, string candidate) { string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar; return candidate.StartsWith(r, StringComparison.OrdinalIgnoreCase); }
        private static string Relative(string root, string path) { return path.Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar); }
        private static string HashFile(string file) { using (SHA256 hash = SHA256.Create()) using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", String.Empty); }
        private static string ShortHash(string text) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToUpperInvariant()))).Replace("-", String.Empty).Substring(0, 24); }
        private static void DeleteQuietly(string file) { try { if (!String.IsNullOrEmpty(file) && File.Exists(file)) File.Delete(file); } catch { } }
        private static void DeleteDirectoryQuietly(string directory) { try { if (!String.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true); } catch { } }

        private sealed class ArchiveFile { public ZipArchiveEntry Entry; public string RelativePath; public string StagedPath; }
        private sealed class PlannedWrite { public ArchiveFile File; public string Destination; public bool Replace; public string OriginalHash; public string Backup; public bool Committed; public string CommittedHash; public SnippetArchiveChange Change; }
    }
}
