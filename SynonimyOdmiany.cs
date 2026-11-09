// SynonimyOdmiany.cs - odczyt indeksu odmiany bez SQL i zewnetrznego runtime'u.
// EDSYNF01/02: naglowek 28B, rekordy UTF-8, katalog FNV-1a64/offset.
// Wersja 02 kompresuje kazdy blok wynikow osobno, zachowujac dostep losowy.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class SynonimyOdmiany {
static readonly UTF8Encoding utf8 = new UTF8Encoding(false, true);

static ulong Hash(string sValue) {
ulong hash = 14695981039346656037UL;
foreach (byte value in utf8.GetBytes(sValue)) { hash ^= value; hash = unchecked(hash * 1099511628211UL); }
return hash;
} // Hash method

static string ReadText(BinaryReader reader, long iDirectory, int iMaximum) {
if (reader.BaseStream.Position > iDirectory - 4) throw new InvalidDataException("Truncated morphology record.");
int iSize = reader.ReadInt32();
if (iSize < 0 || iSize > iMaximum || iSize > iDirectory - reader.BaseStream.Position) throw new InvalidDataException("Invalid morphology text length.");
byte[] data = reader.ReadBytes(iSize);
if (data.Length != iSize) throw new InvalidDataException("Truncated morphology text.");
return utf8.GetString(data);
} // ReadText method

static string ReadCompressedText(BinaryReader reader, long iDirectory) {
if (reader.BaseStream.Position > iDirectory - 8) throw new InvalidDataException("Truncated compressed morphology record.");
int iPacked = reader.ReadInt32();
int iLength = reader.ReadInt32();
if (iPacked < 18 || iPacked > 4 * 1024 * 1024 + 65536 || iLength < 0 || iLength > 4 * 1024 * 1024 || iPacked > iDirectory - reader.BaseStream.Position) throw new InvalidDataException("Invalid compressed morphology length.");
byte[] packed = reader.ReadBytes(iPacked);
if (packed.Length != iPacked) throw new InvalidDataException("Truncated compressed morphology block.");
byte[] data = new byte[iLength];
using (MemoryStream memory = new MemoryStream(packed, false))
using (GZipStream gzip = new GZipStream(memory, CompressionMode.Decompress)) {
int iRead = 0;
while (iRead < data.Length) {
int count = gzip.Read(data, iRead, data.Length - iRead);
if (count == 0) throw new InvalidDataException("Truncated expanded morphology block.");
iRead += count;
}
if (gzip.ReadByte() != -1) throw new InvalidDataException("Expanded morphology block exceeds declared length.");
}
return utf8.GetString(data);
} // ReadCompressedText method

public static List<string[]> Lookup(string sPath, string sKey) {
List<string[]> result = new List<string[]>();
using (FileStream stream = new FileStream(sPath, FileMode.Open, FileAccess.Read, FileShare.Read))
using (BinaryReader reader = new BinaryReader(stream, utf8)) {
if (stream.Length < 28) throw new InvalidDataException("Invalid morphology header.");
string sFormat = Encoding.ASCII.GetString(reader.ReadBytes(8));
if (sFormat != "EDSYNF01" && sFormat != "EDSYNF02") throw new InvalidDataException("Invalid morphology header.");
uint iCount = reader.ReadUInt32();
ulong uDirectory = reader.ReadUInt64();
ulong uLength = reader.ReadUInt64();
if (uLength != (ulong) stream.Length || uDirectory < 28 || uDirectory > uLength || (ulong) iCount * 16 != uLength - uDirectory) throw new InvalidDataException("Invalid morphology directory.");
long iDirectory = (long) uDirectory;
ulong target = Hash(sKey);
long iLow = 0, iHigh = iCount;
while (iLow < iHigh) {
long iMiddle = iLow + (iHigh - iLow) / 2;
stream.Position = iDirectory + iMiddle * 16;
if (reader.ReadUInt64() < target) iLow = iMiddle + 1;
else iHigh = iMiddle;
}
for (long iIndex = iLow; iIndex < iCount; iIndex++) {
stream.Position = iDirectory + iIndex * 16;
if (reader.ReadUInt64() != target) break;
ulong uRecord = reader.ReadUInt64();
if (uRecord < 28 || uRecord >= uDirectory) throw new InvalidDataException("Invalid morphology record offset.");
stream.Position = (long) uRecord;
string sStoredKey = ReadText(reader, iDirectory, 4096);
if (sStoredKey != sKey) continue; // Rozstrzygamy kolizje skrotu pelnym kluczem.
string sValue = sFormat == "EDSYNF02" ? ReadCompressedText(reader, iDirectory) : ReadText(reader, iDirectory, 4 * 1024 * 1024);
foreach (string sRow in sValue.Split(new char[] {'\n'})) {
if (sRow.Length == 0) continue;
string[] fields = sRow.Split(new char[] {'\t'});
if (fields.Length != 4 || !Synonimy.IsSingleWord(fields[0]) || fields[0].IndexOf('\0') >= 0 || fields[0].IndexOf('|') >= 0 || (fields[1] != "n" && fields[1] != "v" && fields[1] != "a" && fields[1] != "r") || fields[2].Length != 32 || fields[3].Length == 0 || sRow.IndexOf('\r') >= 0) throw new InvalidDataException("Invalid morphology analysis.");
foreach (char c in fields[2]) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) throw new InvalidDataException("Invalid morphology sense identifier.");
result.Add(fields);
}
return result;
}
}
return result;
} // Lookup method

static bool Intersects(string sFirst, string sSecond) {
foreach (string a in sFirst.Split(new char[] {'.'})) foreach (string b in sSecond.Split(new char[] {'.'})) if (a == b && a.Length > 0) return true;
return false;
} // Intersects method

public static bool TagsMatch(string sPart, string sSource, string sTarget) {
string[] source = sSource.Split(new char[] {':'});
string[] target = sTarget.Split(new char[] {':'});
if (sPart == "n" && source.Length == 4 && target.Length == 4 && source[0] == "subst" && target[0] == "subst") return Intersects(source[1], target[1]) && Intersects(source[2], target[2]);
if (sPart == "r" && source.Length == 2 && target.Length == 2 && source[0] == "adv" && target[0] == "adv") return Intersects(source[1], target[1]);
if (sPart == "a" && source.Length == 5 && target.Length == 5 && source[0] == "adj" && target[0] == "adj") {
for (int i = 1; i < 5; i++) if (!Intersects(source[i], target[i])) return false;
return true;
}
if (sPart == "v" && source.Length >= 2 && source.Length == target.Length && source[0] == target[0] && ("|inf|fin|bedzie|impt|praet|imps|pcon|pant|ger|ppas|pact|winien|").IndexOf("|" + source[0] + "|", StringComparison.Ordinal) >= 0) {
for (int i = 1; i < source.Length; i++) if (!Intersects(source[i], target[i])) return false;
return true;
}
return false;
} // TagsMatch method
} // SynonimyOdmiany class
