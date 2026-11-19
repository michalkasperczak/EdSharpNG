// ProblemReport.cs -- redacted, local-first Sygnalista client core for EdSharpNG.
// This file deliberately has no dependency on the WinForms report dialog so its
// privacy, serialization and transport contract can be tested without GUI or network.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public class ProblemReportDraft {
public string Subject = "";
public string Description = "";
public string ContactEmail = "";
public bool ContactConsented = false;
public bool IncludeDiagnosticLog = false;
public string DiagnosticLogText = "";
}

public class ProblemReportTechnical {
public string Version;
public string Windows;
public string DotNet;
public ProblemReportTechnical(string sVersion, string sWindows, string sDotNet) {
Version = sVersion ?? ""; Windows = sWindows ?? ""; DotNet = sDotNet ?? "";
}
}

public class ProblemReportSnapshot {
public string ReportId;
public string Title;
public string ReportText;
public string ContactEmail;
public string Channel;
public bool IncludedLog;
public bool IsValid;
public string ValidationError;
public byte[] Utf8Bytes;
public byte[] PayloadUtf8Bytes;
}

public class ProblemReportHttpResponse {
public int StatusCode;
public string Body;
public string Error;
public ProblemReportHttpResponse(int iStatusCode, string sBody, string sError) {
StatusCode = iStatusCode; Body = sBody ?? ""; Error = sError ?? "";
}
}

public interface IProblemReportTransport {
ProblemReportHttpResponse Post(Uri uEndpoint, byte[] abBody);
}

public enum ProblemReportSendCode {
LocalOnly,
Accepted,
Rejected,
Uncertain,
TransportFailure,
ValidationFailure
}

public class ProblemReportSendResult {
public bool Success;
public bool Ambiguous;
public string Message;
public int IssueNumber;
public string IssueUrl;
public ProblemReportSendCode Code;
}

public static class ProblemReportRedactor {
private static readonly Regex reEmail = new Regex("\\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}\\b", RegexOptions.IgnoreCase);
private static readonly Regex reWindowsPath = new Regex("\\b[A-Z]:\\\\(?:[^\\\\\\r\\n<>:\"|?*]+\\\\)*[^\\\\\\r\\n<>:\"|?*]*", RegexOptions.IgnoreCase);
private static readonly Regex reUnixPath = new Regex("(?<![A-Z0-9])/(?:[^\\s/]+/)+[^\\s]*", RegexOptions.IgnoreCase);
private static readonly Regex reIp = new Regex("\\b(?:(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})\\.){3}(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})\\b");
private static readonly Regex reMac = new Regex("\\b(?:[0-9A-F]{2}[:-]){5}[0-9A-F]{2}\\b", RegexOptions.IgnoreCase);
private static readonly Regex reUrl = new Regex("\\b(?:https?|ftp)://[^\\s<>\"']+", RegexOptions.IgnoreCase);
private static readonly Regex reSecret = new Regex("\\b(token|password|passwd|secret|api[_-]?key|authorization)\\s*[:=]\\s*[^\\s,;]+", RegexOptions.IgnoreCase);
private static readonly Regex reBearer = new Regex("\\bBearer\\s+[A-Za-z0-9._~+/-]+", RegexOptions.IgnoreCase);
private static readonly Regex reUncPath = new Regex("(?<!\\S)\\\\\\\\[^\\s\\\\]+\\\\(?:[^\\r\\n<>:\"|?*\\\\]+\\\\?)*", RegexOptions.IgnoreCase);
private static readonly Regex reIpv6 = new Regex("(?<![A-F0-9:])(?:\\[[0-9A-F:]{2,}\\]|(?:[0-9A-F]{1,4}:){2,}[0-9A-F:]+)(?![A-F0-9:])", RegexOptions.IgnoreCase);
private static readonly Regex reBasicAuthorization = new Regex("\\bAuthorization\\s*:\\s*Basic\\s+[^\\s,;]+", RegexOptions.IgnoreCase);
private static readonly Regex reGithubToken = new Regex("\\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})\\b", RegexOptions.IgnoreCase);
private static readonly Regex reJsonSecret = new Regex("(\\\"(?:token|password|passwd|secret|api[_-]?key|authorization)\\\"\\s*:\\s*\\\")[^\\\"]*", RegexOptions.IgnoreCase);

public static string Redact(string sText, string sExplicitContact) {
string s = sText ?? "";
List<string> lContacts = new List<string>();
if (!string.IsNullOrEmpty(sExplicitContact)) {
string sPattern = Regex.Escape(sExplicitContact.Trim());
if (sPattern.Length > 0) {
s = Regex.Replace(s, sPattern, delegate(Match m) {
lContacts.Add(m.Value);
return "@@PRIVATECONTACT" + (lContacts.Count - 1).ToString() + "@@";
}, RegexOptions.IgnoreCase);
}
}
s = reUrl.Replace(s, "[adres usunięty]");
s = reWindowsPath.Replace(s, "[ścieżka usunięta]");
s = reUncPath.Replace(s, "[ścieżka usunięta]");
s = reUnixPath.Replace(s, "[ścieżka usunięta]");
s = reIp.Replace(s, "[IP usunięte]");
s = reIpv6.Replace(s, "[IP usunięte]");
s = reMac.Replace(s, "[MAC usunięty]");
s = reEmail.Replace(s, "[e-mail usunięty]");
s = reBasicAuthorization.Replace(s, delegate(Match m) { return "Authorization: Basic " + "[" + "sekret usunięty" + "]"; });
s = reBearer.Replace(s, "Bearer [sekret usunięty]");
s = reGithubToken.Replace(s, "[sekret usunięty]");
s = reJsonSecret.Replace(s, "$1[sekret usunięty]");
s = reSecret.Replace(s, "$1=[sekret usunięty]");
for (int i = 0; i < lContacts.Count; i++) s = s.Replace("@@PRIVATECONTACT" + i.ToString() + "@@", lContacts[i]);
return s;
}

public static bool IsValidEmail(string sEmail) {
if (string.IsNullOrEmpty(sEmail)) return true;
string sTrimmed = sEmail.Trim();
return sTrimmed.Length <= 254 && reEmail.IsMatch(sTrimmed) && string.Equals(reEmail.Match(sTrimmed).Value, sTrimmed, StringComparison.OrdinalIgnoreCase);
}
}

public static class ProblemReportService {
public const int MaxPayloadBytes = 60 * 1024;
private const string ExpectedIssueHost = "github.com";
private const string ExpectedIssuePath = "/michalkasperczak/EdSharpNG/issues/";

public static ProblemReportSnapshot CreateSnapshot(ProblemReportDraft d, ProblemReportTechnical t, ProblemReportSnapshot previous) {
return CreateSnapshot(d, t, previous, "stable");
}

public static string ResolveChannel(string sProfile) {
return string.Equals((sProfile ?? "").Trim(), "test", StringComparison.OrdinalIgnoreCase) ? "test" : "stable";
}

public static ProblemReportSnapshot CreateSnapshot(ProblemReportDraft d, ProblemReportTechnical t, ProblemReportSnapshot previous, string sChannel) {
if (d == null) throw new ArgumentNullException("d");
if (t == null) throw new ArgumentNullException("t");
string sResolvedChannel = ResolveChannel(sChannel);
string sContact = (d.ContactEmail ?? "").Trim();
if (d.ContactConsented && !ProblemReportRedactor.IsValidEmail(sContact)) throw new ArgumentException(JezykInterfejsu.Wybierz("Invalid email address.", "Nieprawidłowy adres e-mail."));
if (!d.ContactConsented) sContact = "";
string sTitle = ProblemReportRedactor.Redact((d.Subject ?? "").Trim(), "");
string sDescription = ProblemReportRedactor.Redact((d.Description ?? "").Trim(), "");
if (sTitle.Length == 0 && sDescription.Length == 0) throw new ArgumentException(JezykInterfejsu.Wybierz("A report description is required.", "Brak opisu zgłoszenia."));
if (sTitle.Length == 0) sTitle = JezykInterfejsu.Wybierz("(no subject)", "(bez tematu)");
string sLog = "";
if (d.IncludeDiagnosticLog) sLog = ProblemReportRedactor.Redact(d.DiagnosticLogText ?? "", "").Trim();
string sContactLine = sContact.Length == 0 ? "" : JezykInterfejsu.Wybierz("Private contact: ", "Kontakt prywatny: ") + sContact + "\r\n";
string sLogSection = d.IncludeDiagnosticLog
    ? JezykInterfejsu.Wybierz("\r\n--- Safe diagnostic log ---\r\n", "\r\n--- Bezpieczny dziennik diagnostyczny ---\r\n")
        + (sLog.Length == 0 ? JezykInterfejsu.Wybierz("(no entries)", "(brak wpisów)") : sLog) + "\r\n"
    : "";
string sTemplate = JezykInterfejsu.Wybierz(
    "EdSharpNG technical report (private)\r\nSubject: {0}\r\n{1}\r\nDescription:\r\n{2}\r\n{3}\r\n--- Technical data ---\r\nVersion: {4}\r\nWindows: {5}\r\n.NET: {6}\r\nPrivacy notice: the full report goes to the private reporting mailbox. The public summary contains neither description nor contact.\r\n",
    "Zgłoszenie techniczne EdSharpNG (prywatne)\r\nTemat: {0}\r\n{1}\r\nOpis:\r\n{2}\r\n{3}\r\n--- Dane techniczne ---\r\nWersja: {4}\r\nWindows: {5}\r\n.NET: {6}\r\nInformacja o prywatności: pełny raport trafia do prywatnej skrzynki zgłoszeń. Publiczne streszczenie nie zawiera opisu ani kontaktu.\r\n");
string sText = String.Format(sTemplate, sTitle, sContactLine, sDescription, sLogSection,
    ProblemReportRedactor.Redact(t.Version, ""), ProblemReportRedactor.Redact(t.Windows, ""), ProblemReportRedactor.Redact(t.DotNet, ""));
string sFingerprint = sTitle + "\n" + sText + "\n" + sContact + "\n" + (d.IncludeDiagnosticLog ? "Y" : "N") + "\n" + sResolvedChannel;
string sId = (previous != null && string.Equals(previous.ValidationError, sFingerprint, StringComparison.Ordinal)) ? previous.ReportId : Guid.NewGuid().ToString();
ProblemReportSnapshot snapshot = new ProblemReportSnapshot();
snapshot.ReportId = sId; snapshot.Title = sTitle; snapshot.ReportText = sText; snapshot.ContactEmail = sContact; snapshot.Channel = sResolvedChannel;
snapshot.IncludedLog = d.IncludeDiagnosticLog; snapshot.IsValid = true; snapshot.ValidationError = sFingerprint;
snapshot.Utf8Bytes = new UTF8Encoding(false).GetBytes(sText);
snapshot.PayloadUtf8Bytes = new UTF8Encoding(false).GetBytes(BuildJson(snapshot));
if (snapshot.PayloadUtf8Bytes.Length > MaxPayloadBytes) throw new ArgumentException(JezykInterfejsu.Wybierz("The report exceeds the 60 KiB UTF-8 limit.", "Zgłoszenie przekracza limit 60 KiB UTF-8."));
return snapshot;
}

public static string BuildJson(ProblemReportSnapshot s) {
if (s == null) throw new ArgumentNullException("s");
StringBuilder sb = new StringBuilder();
sb.Append("{\"schemaVersion\":1");
sb.Append(",\"reportId\":\"").Append(JsonEscape(s.ReportId)).Append("\"");
sb.Append(",\"kind\":\"bug\"");
sb.Append(",\"title\":\"").Append(JsonEscape(s.Title)).Append("\"");
sb.Append(",\"reportText\":\"").Append(JsonEscape(s.ReportText)).Append("\"");
sb.Append(",\"includedLog\":").Append(s.IncludedLog ? "true" : "false");
sb.Append(",\"channel\":\"").Append(ResolveChannel(s.Channel)).Append("\"");
if (!string.IsNullOrEmpty(s.ContactEmail)) sb.Append(",\"contactEmail\":\"").Append(JsonEscape(s.ContactEmail)).Append("\"");
sb.Append("}");
return sb.ToString();
}

public static string JsonEscape(string sText) {
if (sText == null) return "";
StringBuilder sb = new StringBuilder();
foreach (char c in sText) {
switch (c) {
case '"': sb.Append("\\\""); break;
case '\\': sb.Append("\\\\"); break;
case '\r': sb.Append("\\r"); break;
case '\n': sb.Append("\\n"); break;
case '\t': sb.Append("\\t"); break;
default: if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
}
}
return sb.ToString();
}

public static bool TryBuildEndpoint(string sValue, out Uri uEndpoint) {
uEndpoint = null;
if (string.IsNullOrEmpty(sValue)) return false;
Uri u;
if (!Uri.TryCreate(sValue.Trim(), UriKind.Absolute, out u)) return false;
if (!string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
if (!string.IsNullOrEmpty(u.UserInfo) || !string.IsNullOrEmpty(u.Query) || !string.IsNullOrEmpty(u.Fragment)) return false;
if (u.Host.IndexOf("amc", StringComparison.OrdinalIgnoreCase) >= 0) return false;
uEndpoint = u;
return true;
}

public static string ReadEndpoint(string sDirectory, string sProfile) {
string sName = ResolveChannel(sProfile) == "test" ? "problem-report-endpoint-test.txt" : "problem-report-endpoint-public.txt";
try {
string sFile = Path.Combine(sDirectory ?? "", sName);
if (!File.Exists(sFile)) return "";
string sValue = File.ReadAllText(sFile, Encoding.UTF8).Trim();
Uri u;
return TryBuildEndpoint(sValue, out u) ? u.AbsoluteUri : "";
}
catch { return ""; }
}

public static string SaveAtomically(string sDirectory, ProblemReportSnapshot s) {
if (s == null || s.Utf8Bytes == null) throw new ArgumentException(JezykInterfejsu.Wybierz("No report snapshot is available.", "Brak migawki zgłoszenia."));
Directory.CreateDirectory(sDirectory);
string sBase = "report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + s.ReportId + ".txt";
string sPath = Path.Combine(sDirectory, sBase);
int i = 1;
while (File.Exists(sPath)) { sPath = Path.Combine(sDirectory, Path.GetFileNameWithoutExtension(sBase) + "-" + i.ToString() + ".txt"); i++; }
string sTemp = sPath + ".tmp-" + Guid.NewGuid().ToString("N");
try {
using (FileStream fs = new FileStream(sTemp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { fs.Write(s.Utf8Bytes, 0, s.Utf8Bytes.Length); fs.Flush(true); }
File.Move(sTemp, sPath);
return sPath;
}
finally { try { if (File.Exists(sTemp)) File.Delete(sTemp); } catch {} }
}

public static ProblemReportSendResult Send(string sEndpoint, ProblemReportSnapshot s, IProblemReportTransport transport) {
ProblemReportSendResult r = new ProblemReportSendResult();
Uri u;
if (s == null) { r.Code = ProblemReportSendCode.ValidationFailure; r.Message = JezykInterfejsu.Wybierz("No report snapshot is available.", "Brak migawki zgłoszenia."); return r; }
if (!TryBuildEndpoint(sEndpoint, out u)) { r.Code = ProblemReportSendCode.LocalOnly; r.Message = JezykInterfejsu.Wybierz("No receiving endpoint is configured.", "Punkt odbiorczy nie jest skonfigurowany."); return r; }
if (transport == null) { r.Code = ProblemReportSendCode.ValidationFailure; r.Message = JezykInterfejsu.Wybierz("Unable to send the report.", "Nie udało się wysłać zgłoszenia."); return r; }
try {
ProblemReportHttpResponse response = transport.Post(u, s.PayloadUtf8Bytes);
if (response == null) { r.Code = ProblemReportSendCode.Uncertain; r.Ambiguous = true; r.Message = JezykInterfejsu.Wybierz("No response from the receiver. The report was not sent again automatically.", "Brak odpowiedzi serwera. Nie wysłano ponownie automatycznie."); return r; }
if (response.StatusCode == 502 || response.StatusCode == 503 || response.StatusCode == 0) { r.Code = ProblemReportSendCode.Uncertain; r.Ambiguous = true; r.Message = JezykInterfejsu.Wybierz("The sending result is uncertain. The report was not sent again automatically.", "Niepewny wynik wysyłki. Nie wysłano ponownie automatycznie."); return r; }
if (response.StatusCode != 201) { r.Code = ProblemReportSendCode.Rejected; r.Message = String.Format(JezykInterfejsu.Wybierz("The receiver declined the report (HTTP {0}).", "Odbiorca odmówił przyjęcia zgłoszenia (HTTP {0})."), response.StatusCode); return r; }
int iIssue; string sIssueUrl; string sResponseId;
if (!TryParseSuccess(response.Body, out iIssue, out sIssueUrl, out sResponseId) || (sResponseId.Length > 0 && !string.Equals(s.ResponseIdOrEmpty(), sResponseId, StringComparison.OrdinalIgnoreCase))) { r.Code = ProblemReportSendCode.Uncertain; r.Ambiguous = true; r.Message = JezykInterfejsu.Wybierz("The receiver response does not confirm a valid report.", "Odpowiedź serwera nie potwierdza poprawnego zgłoszenia."); return r; }
r.Code = ProblemReportSendCode.Accepted; r.Success = true; r.IssueNumber = iIssue; r.IssueUrl = sIssueUrl; r.Message = JezykInterfejsu.Wybierz("The report was sent.", "Zgłoszenie wysłano."); return r;
}
catch (WebException) { r.Code = ProblemReportSendCode.TransportFailure; r.Ambiguous = true; r.Message = JezykInterfejsu.Wybierz("There is no definite confirmation that the report was sent. The report was not sent again automatically.", "Brak pewnego potwierdzenia wysyłki. Nie wysłano ponownie automatycznie."); return r; }
catch { r.Code = ProblemReportSendCode.TransportFailure; r.Message = JezykInterfejsu.Wybierz("Unable to send the report.", "Nie udało się wysłać zgłoszenia."); return r; }
}

private static string ResponseIdOrEmpty(this ProblemReportSnapshot s) { return s.ReportId ?? ""; }

private static bool TryParseSuccess(string sJson, out int iIssue, out string sUrl, out string sReportId) {
iIssue = 0; sUrl = ""; sReportId = "";
Match mNumber = Regex.Match(sJson ?? "", "\\\"issueNumber\\\"\\s*:\\s*([1-9][0-9]*)");
Match mUrl = Regex.Match(sJson ?? "", "\\\"issueUrl\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
Match mId = Regex.Match(sJson ?? "", "\\\"reportId\\\"\\s*:\\s*\\\"([0-9a-fA-F-]{36})\\\"");
if (!mNumber.Success || !mUrl.Success || !Int32.TryParse(mNumber.Groups[1].Value, out iIssue)) return false;
Uri u;
if (!Uri.TryCreate(mUrl.Groups[1].Value, UriKind.Absolute, out u)) return false;
if (!string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !string.Equals(u.Host, ExpectedIssueHost, StringComparison.OrdinalIgnoreCase) || !string.Equals(u.AbsolutePath, ExpectedIssuePath + iIssue.ToString(), StringComparison.Ordinal) || !string.IsNullOrEmpty(u.Query) || !string.IsNullOrEmpty(u.Fragment) || !string.IsNullOrEmpty(u.UserInfo)) return false;
sUrl = u.AbsoluteUri; if (mId.Success) sReportId = mId.Groups[1].Value; return true;
}

public static bool BytesEqual(byte[] a, byte[] b) {
if (a == null || b == null || a.Length != b.Length) return false;
for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
return true;
}
}

public class ProblemReportHttpTransport : IProblemReportTransport {
public ProblemReportHttpResponse Post(Uri uEndpoint, byte[] abBody) {
HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uEndpoint);
request.Method = "POST"; request.ContentType = "application/json; charset=utf-8"; request.AllowAutoRedirect = false;
request.Timeout = 15000; request.ReadWriteTimeout = 15000; request.ContentLength = abBody == null ? 0 : abBody.Length;
try {
using (Stream output = request.GetRequestStream()) { if (abBody != null) output.Write(abBody, 0, abBody.Length); }
using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) return new ProblemReportHttpResponse((int)response.StatusCode, reader.ReadToEnd(), "");
}
catch (WebException ex) {
HttpWebResponse error = ex.Response as HttpWebResponse;
if (error == null) throw;
using (error) using (StreamReader reader = new StreamReader(error.GetResponseStream(), Encoding.UTF8)) return new ProblemReportHttpResponse((int)error.StatusCode, reader.ReadToEnd(), "");
}
}
}
