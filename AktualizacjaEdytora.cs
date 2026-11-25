// One-off update snapshots do not depend on RestoreSession or AutoSaveSeconds.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace EdSharp {
public partial class MdiFrame {
public UpdateSession CaptureUpdateSession(string installer, string checksum, string version) {
UpdateSession session = new UpdateSession();
session.InstallerPath = installer;
session.InstallerSha256 = (checksum ?? "").ToLowerInvariant();
session.TargetVersion = version;
session.ApplicationPath = Application.ExecutablePath;
using (Process process = Process.GetCurrentProcess()) {
session.ParentPid = process.Id;
session.ParentStartTicksUtc = process.StartTime.ToUniversalTime().Ticks;
}
session.Documents = new List<UpdateDocument>();
List<MdiChild> windows = new List<MdiChild>();
foreach (Form form in this.MdiChildren) {
MdiChild child = form as MdiChild;
if (child == null || child.IsDisposed || child.RTB == null)
throw new IOException(JezykInterfejsu.Wybierz("A document cannot be copied safely. The update was not started.", "Nie można bezpiecznie skopiować dokumentu. Aktualizacja nie została rozpoczęta."));
windows.Add(child);
}
windows.Sort(delegate(MdiChild a, MdiChild b) { return a.OpenSequence.CompareTo(b.OpenSequence); });
session.ActiveDocument = -1;
foreach (MdiChild child in windows) {
HomerRichTextBox rtb = child.RTB;
UpdateDocument doc = new UpdateDocument();
doc.File = child.File ?? "";
doc.Title = child.Text ?? "";
doc.ImportedFrom = child.ImportedFrom ?? "";
doc.OriginalPath = child.OriginalDocument == null ? "" : child.OriginalDocument.Path;
doc.OriginalFingerprint = child.OriginalDocument == null ? "" : child.OriginalDocument.Fingerprint;
doc.Text = rtb.Text;
doc.Rtf = rtb.Rtf;
doc.SelectionStart = rtb.SelectionStart;
doc.SelectionLength = rtb.SelectionLength;
doc.Modified = rtb.Modified;
doc.IsRichTextDocument = child.IsRichTextDocument;
doc.IsUnixLineBreak = child.IsUnixLineBreak;
doc.FileLineBreakKind = child.FileLineBreakKind;
doc.ReadOnly = GetUserGuard(child);
doc.Bookmarks = App.ReadValue("Bookmarks", doc.File, "");
doc.EncodingCodePage = child.YieldEncoding == null ? 0 : child.YieldEncoding.CodePage;
doc.EncodingHasBom = child.YieldEncoding != null && child.YieldEncoding.GetPreamble().Length > 0;
doc.MarkdownReview = child.MarkdownReviewMode;
doc.MarkdownNoSync = child.MarkdownReviewNoSync;
doc.ViewSelectionStart = child.MarkdownReviewView == null ? 0 : child.MarkdownReviewView.SelectionStart;
doc.ViewSelectionLength = child.MarkdownReviewView == null ? 0 : child.MarkdownReviewView.SelectionLength;
doc.WindowState = (int)child.WindowState;
// MdiChild w 5.0.134 nie przechowuje niezależnego czasu pliku.
// Verify the actual rich-text round trip BEFORE any document is closed.
using (RichTextBox check = new RichTextBox()) {
check.Rtf = doc.Rtf;
if (!String.Equals(check.Text, doc.Text, StringComparison.Ordinal))
throw new IOException(JezykInterfejsu.Wybierz("A document could not be copied without changing its text.", "Nie można skopiować dokumentu bez zmiany jego tekstu."));
}
if (child == this.Child) session.ActiveDocument = session.Documents.Count;
session.Documents.Add(doc);
}
return session;
} // CaptureUpdateSession method

bool UpdateFolderForToken(string token, out string folder) {
folder = "";
if (String.IsNullOrEmpty(token)) return false;
Guid id;
if (!Guid.TryParseExact(token, "N", out id) || token != id.ToString("N")) return false;
try {
string root = Path.GetFullPath(Path.Combine(App.DataDir, "Aktualizacja"));
string candidate = Path.GetFullPath(Path.Combine(root, token));
if (!String.Equals(Path.GetDirectoryName(candidate), root, StringComparison.OrdinalIgnoreCase)) return false;
if (!Directory.Exists(candidate)) return false;
folder = candidate;
return true;
}
catch { return false; }
} // UpdateFolderForToken method

Encoding RestoreUpdateEncoding(UpdateDocument doc) {
if (doc.EncodingCodePage == 0) return null;
if (doc.EncodingCodePage == 667) return new MazoviaEncoding();
if (doc.EncodingCodePage == 65001) return new UTF8Encoding(doc.EncodingHasBom);
return Encoding.GetEncoding(doc.EncodingCodePage);
} // RestoreUpdateEncoding method

void ValidateUpdateDocument(UpdateDocument doc) {
if (doc == null) throw new InvalidDataException(JezykInterfejsu.Wybierz("The update checkpoint has an invalid document.", "Checkpoint aktualizacji zawiera nieprawidłowy dokument."));
if (doc.SelectionStart > doc.Text.Length || doc.SelectionLength > doc.Text.Length - doc.SelectionStart)
throw new InvalidDataException(JezykInterfejsu.Wybierz("The update checkpoint has an invalid selection.", "Checkpoint aktualizacji zawiera nieprawidłowe zaznaczenie."));
RestoreUpdateEncoding(doc);
if (doc.FileTimeTicks < DateTime.MinValue.Ticks || doc.FileTimeTicks > DateTime.MaxValue.Ticks || doc.FileTimeKind < 0 || doc.FileTimeKind > 2)
throw new InvalidDataException(JezykInterfejsu.Wybierz("The update checkpoint has an invalid file timestamp.", "Checkpoint aktualizacji zawiera nieprawidłowy czas pliku."));
using (RichTextBox check = new RichTextBox()) {
check.Rtf = doc.Rtf;
if (!String.Equals(check.Text, doc.Text, StringComparison.Ordinal))
throw new InvalidDataException(JezykInterfejsu.Wybierz("A document checkpoint does not preserve its text.", "Checkpoint dokumentu nie zachowuje jego tekstu."));
}
} // ValidateUpdateDocument method

void SetUpdateResumePointer(string token) {
string root = Path.Combine(App.DataDir, "Aktualizacja");
Directory.CreateDirectory(root);
string file = Path.Combine(root, "resume-update.txt");
string temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
try {
byte[] bytes = new UTF8Encoding(false).GetBytes(token + "\n");
using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) {
stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
}
if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file);
if (File.ReadAllText(file, new UTF8Encoding(false)).Trim() != token) throw new IOException(JezykInterfejsu.Wybierz("The update recovery pointer could not be verified.", "Nie można potwierdzić wskaźnika odzyskiwania aktualizacji."));
} finally { if (File.Exists(temp)) File.Delete(temp); }
} // SetUpdateResumePointer method

bool IsUpdateParentAlive(UpdateSession session) {
try {
using (Process parent = Process.GetProcessById(session.ParentPid)) {
return !parent.HasExited && parent.StartTime.ToUniversalTime().Ticks == session.ParentStartTicksUtc;
}
} catch (ArgumentException) { return false; }
catch { return true; }
} // IsUpdateParentAlive method

void ClearUpdateResumePointer(string token) {
try {
string file = Path.Combine(App.DataDir, "Aktualizacja", "resume-update.txt");
if (!File.Exists(file)) return;
string saved = File.ReadAllText(file, new UTF8Encoding(false)).Trim();
if (String.Equals(saved, token, StringComparison.Ordinal)) File.Delete(file);
}
catch {}
} // ClearUpdateResumePointer method

public bool TryGetPendingUpdateToken(out string token) {
token = "";
try {
string file = Path.Combine(App.DataDir, "Aktualizacja", "resume-update.txt");
if (!File.Exists(file) || new FileInfo(file).Length > 64) return false;
string value = File.ReadAllText(file, new UTF8Encoding(false)).Trim();
string folder;
if (!UpdateFolderForToken(value, out folder)) return false;
string state = UpdateStore.ReadState(folder);
if (state != "committed" && (state != "prepared" || IsUpdateParentAlive(UpdateStore.Read(folder)))) return false;
token = value;
return true;
}
catch { return false; }
} // TryGetPendingUpdateToken method

public bool RestoreUpdateSession(string token) {
string folder;
if (!UpdateFolderForToken(token, out folder)) return false;
List<MdiChild> created = new List<MdiChild>();
Dictionary<string, string> bookmarksBefore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
List<UpdateDocument> documents;
try {
string state = UpdateStore.ReadState(folder);
UpdateSession session = UpdateStore.Read(folder);
if (state != "committed" && (state != "prepared" || IsUpdateParentAlive(session))) return false;
documents = session.Documents;
foreach (UpdateDocument doc in documents) ValidateUpdateDocument(doc);

foreach (UpdateDocument doc in documents) {
MdiChild child = new MdiChild(this, "NoName");
created.Add(child);
child.File = doc.File;
child.Text = doc.Title;
child.ImportedFrom = doc.ImportedFrom;
if (!String.IsNullOrEmpty(doc.OriginalPath)) child.OriginalDocument = new OriginalDocumentLink {Path = doc.OriginalPath, Fingerprint = doc.OriginalFingerprint};
child.RTB.Rtf = doc.Rtf;
if (!String.Equals(child.RTB.Text, doc.Text, StringComparison.Ordinal)) throw new InvalidDataException(JezykInterfejsu.Wybierz("A restored document changed its text.", "Odtworzony dokument zmienił swój tekst."));
child.IsRichTextDocument = doc.IsRichTextDocument;
child.IsUnixLineBreak = doc.IsUnixLineBreak;
child.FileLineBreakKind = doc.FileLineBreakKind;
child.YieldEncoding = RestoreUpdateEncoding(doc);
child.RTB.Modified = doc.Modified;
child.RTB.SetGuard(doc.ReadOnly);
child.RTB.Select(doc.SelectionStart, doc.SelectionLength);
child.WindowState = (FormWindowState)doc.WindowState;
if (doc.MarkdownReview) {
MarkdownReview_Enter(child, doc.MarkdownNoSync);
if (child.MarkdownReviewView == null || doc.ViewSelectionStart > child.MarkdownReviewView.TextLength || doc.ViewSelectionLength > child.MarkdownReviewView.TextLength - doc.ViewSelectionStart)
throw new InvalidDataException(JezykInterfejsu.Wybierz("The update checkpoint has an invalid preview selection.", "Checkpoint aktualizacji zawiera nieprawidłowe zaznaczenie podglądu."));
child.MarkdownReviewView.Select(doc.ViewSelectionStart, doc.ViewSelectionLength);
}
}

foreach (UpdateDocument doc in documents) {
if (doc.Bookmarks.Length == 0) continue;
if (String.Equals(App.ReadValue("Bookmarks", doc.File, ""), doc.Bookmarks, StringComparison.Ordinal)) continue;
if (!bookmarksBefore.ContainsKey(doc.File)) {
bool existed = false;
foreach (string key in App.ReadSectionKeys("Bookmarks")) if (String.Equals(key, doc.File, StringComparison.OrdinalIgnoreCase)) { existed = true; break; }
bookmarksBefore.Add(doc.File, existed ? App.ReadValue("Bookmarks", doc.File, "") : null);
}
if (!App.WriteValue("Bookmarks", doc.File, doc.Bookmarks) || App.ReadValue("Bookmarks", doc.File, "") != doc.Bookmarks)
throw new IOException(JezykInterfejsu.Wybierz("Bookmarks could not be restored.", "Nie można odtworzyć zakładek."));
}
if (session.ActiveDocument >= 0 && session.ActiveDocument < created.Count) created[session.ActiveDocument].Activate();
RefreshOriginalDocumentTitles();
UpdateStore.SetState(folder, "restored");
ClearUpdateResumePointer(token);
return true;
}
catch {
foreach (KeyValuePair<string, string> bookmark in bookmarksBefore) {
try {
if (bookmark.Value == null) App.DeleteKey("Bookmarks", bookmark.Key);
else App.WriteValue("Bookmarks", bookmark.Key, bookmark.Value);
} catch {}
}
for (int i = created.Count - 1; i >= 0; i--) {
try { if (!created[i].IsDisposed) created[i].Dispose(); } catch {}
}
return false;
}
} // RestoreUpdateSession method

public string GetUpdateResumeMessage(string token) {
string folder;
if (!UpdateFolderForToken(token, out folder)) return JezykInterfejsu.Wybierz("The update result was not confirmed.", "Nie potwierdzono wyniku aktualizacji.");
try {
UpdateSession session = UpdateStore.Read(folder);
UpdateResult result = UpdateStore.ReadResult(folder);
if (result != null && result.Success && result.InstallerExitCode == 0 && Util.CompareVersions(App.VersionString, session.TargetVersion) == 0)
return JezykInterfejsu.Wybierz("Update installed and work restored.", "Zainstalowano aktualizację i odtworzono pracę.");
}
catch {}
return JezykInterfejsu.Wybierz("The update result was not confirmed; work was restored.", "Nie potwierdzono wyniku aktualizacji; odtworzono pracę.");
} // GetUpdateResumeMessage method

public bool TryStartPreparedAutomaticUpdate() {
try {
if (!Ustawienia.CzyWlaczone(App.ReadOption("AutoInstallVerifiedUpdate", "N"))) return false;
AutomaticUpdatePackage package;
if (!AutomaticUpdateStore.TryTakeReady(App.DataDir, App.VersionString, DateTime.UtcNow, out package)) return false;
return InstallVerifiedUpdate(package.InstallerPath, package.InstallerSha256, package.TargetVersion);
}
catch { return false; }
} // TryStartPreparedAutomaticUpdate method

private bool updateInstallInProgress = false;
public bool InstallVerifiedUpdate(string installer, string expectedChecksum, string latestVersion) {
if (updateInstallInProgress) return false;
updateInstallInProgress = true;
string folder = "";
bool exitAccepted = false;
try {
if (String.IsNullOrEmpty(installer) || String.IsNullOrEmpty(expectedChecksum) || expectedChecksum.Length != 64 || !File.Exists(installer)) {
Dialog.Show(JezykInterfejsu.Wybierz("Elevate Version", "Aktualizacja wersji"), JezykInterfejsu.Wybierz("The installer could not be verified. Your documents remain open.", "Nie można potwierdzić instalatora. Dokumenty pozostają otwarte."));
return false;
}
string actual = Util.FileSha256(installer);
if (!String.Equals(actual, expectedChecksum, StringComparison.OrdinalIgnoreCase)) {
Dialog.Show(JezykInterfejsu.Wybierz("Elevate Version", "Aktualizacja wersji"), JezykInterfejsu.Wybierz("The installer could not be verified. Your documents remain open.", "Nie można potwierdzić instalatora. Dokumenty pozostają otwarte."));
return false;
}
UpdateSession session = CaptureUpdateSession(installer, expectedChecksum, latestVersion);
folder = UpdateStore.Create(App.DataDir, session);
SetUpdateResumePointer(Path.GetFileName(folder));
UpdateStore.StartHelper(folder, Path.Combine(App.ProgramDir, "EdSharpUpdate.exe"));
CancelEventArgs exit = new CancelEventArgs();
Application.Exit(exit);
if (exit.Cancel) {
try { UpdateStore.CancelHelper(folder); } catch {}
try { UpdateStore.SetState(folder, "cancelled"); } catch {}
ClearUpdateResumePointer(Path.GetFileName(folder));
Dialog.Show(JezykInterfejsu.Wybierz("Elevate Version", "Aktualizacja wersji"), JezykInterfejsu.Wybierz("The update was cancelled. Your documents remain open.", "Aktualizację anulowano. Dokumenty pozostają otwarte."));
return false;
}
exitAccepted = true;
UpdateStore.SetState(folder, "committed");
return true;
}
catch (Exception ex) {
// Accepted Exit has already closed the windows. Preserve the prepared copy
// for the helper's restart-only path instead of claiming they remain open.
if (exitAccepted || (this.IsDisposed && folder.Length > 0)) return true;
if (folder.Length > 0) try { UpdateStore.CancelHelper(folder); } catch {}
if (folder.Length > 0) try { if (UpdateStore.ReadState(folder) == "prepared") UpdateStore.SetState(folder, "cancelled"); } catch {}
if (folder.Length > 0) ClearUpdateResumePointer(Path.GetFileName(folder));
Dialog.Show(JezykInterfejsu.Wybierz("Elevate Version", "Aktualizacja wersji"), JezykInterfejsu.Wybierz("The update could not be prepared. Your documents remain open.\n", "Nie można przygotować aktualizacji. Dokumenty pozostają otwarte.\n") + ex.Message);
return false;
}
finally { updateInstallInProgress = false; }
} // InstallVerifiedUpdate method
}
}
