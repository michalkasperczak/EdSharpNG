// ProblemReportDialog.cs -- accessible local-first UI around ProblemReport.cs.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EdSharp;
using Homer;

public static class ProblemReportSendGate {
private static int iSending;
public static bool TryBegin() { return Interlocked.CompareExchange(ref iSending, 1, 0) == 0; }
public static void Complete() { Interlocked.Exchange(ref iSending, 0); }
}

public static class ProblemReportDialog {
private static string T(string en, string pl) { return JezykInterfejsu.Wybierz(en, pl); }
private static bool Wybrano(string action, string label) { return string.Equals(action, (label ?? "").Replace("&", ""), StringComparison.OrdinalIgnoreCase); }

public static void Show(MdiFrame frame, string sPreSubject, string sPreDescription) {
ProblemReportDraft draft = new ProblemReportDraft();
draft.Subject = sPreSubject ?? "";
draft.Description = sPreDescription ?? "";
draft.IncludeDiagnosticLog = Ustawienia.CzyWlaczone(App.ReadOption("AttachDiagnosticLog", "N"));
ProblemReportSnapshot previous = null;
string sChannel = ProblemReportService.ResolveChannel(App.ReadData("ProblemReportEndpointProfile", "public"));
bool bSendingActive = ProblemReportService.ReadEndpoint(App.ProgramDir, sChannel).Length > 0;
while (true) {
string sTitle = T("Report a problem", "Zgłoś problem");
string sPreview = T("&Preview", "&Podgląd");
string sCancel = T("&Cancel", "&Anuluj");
LbcDialog edit = new LbcDialog(sTitle, frame);
edit.addLabel(bSendingActive ? T("Your description will first be shown in a redacted preview. Sending is available only to a configured HTTPS endpoint.", "Opis zostanie najpierw pokazany w zredagowanym podglądzie. Wysyłka jest możliwa tylko do skonfigurowanego punktu HTTPS.") : T("Sending is currently inactive. You can compose, copy, or save the report locally; it will not be sent.", "Wysyłka jest obecnie nieaktywna. Zgłoszenie można zredagować, skopiować albo zapisać lokalnie; nie zostanie nadane."));
TextBox subject = edit.addInputBox(T("&Subject", "&Temat"), draft.Subject, T("A short description of the problem.", "Krótki opis problemu."));
TextBox description = edit.addTextMemo(T("&Description", "&Opis"), draft.Description);
TextBox email = edit.addInputBox(T("Optional &email address", "Opcjonalny adres &e-mail"), draft.ContactEmail, T("The address will be shared privately only with your explicit consent.", "Adres zostanie przekazany wyłącznie prywatnie po wyraźnej zgodzie."));
CheckBox consent = edit.addCheckBox(T("I consent to sharing my email address privately", "Wyrażam zgodę na przekazanie adresu e-mail prywatnie"), draft.ContactConsented,
T("Without this consent, the email address will not be included in the report.", "Bez tej zgody adres e-mail nie zostanie dołączony do zgłoszenia."));
bool bSafeLogAvailable = Ustawienia.CzyWlaczone(App.ReadOption("DiagnosticLogging", "Y")) && SafeLogExists();
CheckBox attach = edit.addCheckBox(T("Include a safe &diagnostic log", "Dołącz bezpieczny dziennik &diagnostyczny"), bSafeLogAvailable && draft.IncludeDiagnosticLog,
T("Only a redacted technical log is included; it does not include speech or document contents.", "Dołączany jest tylko zredagowany dziennik techniczny; nie obejmuje mowy ani treści dokumentów."));
if (!bSafeLogAvailable) { attach.Enabled = false; attach.Checked = false; }
string sAction = edit.runWithButtons(new string[] { sPreview, sCancel });
draft.Subject = subject.Text; draft.Description = description.Text; draft.ContactEmail = email.Text; draft.ContactConsented = consent.Checked;
draft.IncludeDiagnosticLog = bSafeLogAvailable && attach.Checked; edit.Dispose();
if (Wybrano(sAction, sCancel) || sAction.Length == 0) return;
try {
draft.DiagnosticLogText = draft.IncludeDiagnosticLog ? ReadSafeLogTail() : "";
ProblemReportTechnical technical = new ProblemReportTechnical(App.VersionString, Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit"), Environment.Version.ToString());
ProblemReportSnapshot snapshot = ProblemReportService.CreateSnapshot(draft, technical, previous, sChannel);
previous = snapshot;
if (!Preview(frame, snapshot)) return;
}
catch (ArgumentException ex) { Dialog.Show(sTitle, ex.Message); }
catch { Dialog.Show(sTitle, T("Unable to create a safe report preview.", "Nie udało się utworzyć bezpiecznego podglądu zgłoszenia.")); }
}
}

private static bool Preview(MdiFrame frame, ProblemReportSnapshot snapshot) {
string sSaved = "";
while (true) {
string sEndpoint = ProblemReportService.ReadEndpoint(App.ProgramDir, snapshot.Channel);
string sTitle = T("Report preview", "Podgląd zgłoszenia");
string sCopy = T("&Copy", "&Kopiuj");
string sSave = T("Save &locally", "Zapisz &lokalnie");
string sSend = T("&Send", "&Wyślij");
string sBack = T("&Return to editing", "&Wróć do edycji");
LbcDialog preview = new LbcDialog(sTitle, frame);
preview.addLabel(sEndpoint.Length == 0 ? T("Sending is currently inactive. This is the complete redacted snapshot. Copying and saving use this same unchanged snapshot; neither action sends the report.", "Wysyłka jest obecnie nieaktywna. To jest pełna zredagowana migawka. Kopiowanie i zapis używają tej samej migawki bez zmian; żadna z tych czynności nie wysyła zgłoszenia.") : T("This is the complete redacted snapshot. Before sending, you can copy it or save it locally; those actions use this same unchanged snapshot.", "To jest pełna zredagowana migawka. Przed wysłaniem możesz ją skopiować albo zapisać lokalnie; te czynności używają tej samej migawki bez zmian."));
TextBox text = preview.addTextMemo(T("Redacted report", "Zredagowany raport"), snapshot.ReportText);
text.ReadOnly = true;
string[] aButtons = sEndpoint.Length == 0 ? new string[] { sCopy, sSave, sBack } : new string[] { sCopy, sSave, sSend, sBack };
string sAction = preview.runWithButtons(aButtons);
preview.Dispose();
if (Wybrano(sAction, sBack) || sAction.Length == 0) return true;
if (Wybrano(sAction, sCopy)) {
try { Util.SetClipboardText(Encoding.UTF8.GetString(snapshot.Utf8Bytes)); Dialog.Show(sTitle, T("The redacted snapshot was copied.", "Zredagowaną migawkę skopiowano.")); }
catch { Dialog.Show(sTitle, T("Unable to copy the snapshot.", "Nie udało się skopiować migawki.")); }
continue;
}
if (Wybrano(sAction, sSave)) {
try { sSaved = ProblemReportService.SaveAtomically(Path.Combine(App.DataDir, "Reports"), snapshot); Dialog.Show(sTitle, T("A local copy was saved. The report was not sent.\r\nPath: ", "Zapisano lokalną kopię. Nie wysłano zgłoszenia.\r\nŚcieżka: ") + sSaved); }
catch { Dialog.Show(sTitle, T("Unable to save the report copy. Sending was not started.", "Nie udało się zapisać kopii zgłoszenia. Wysyłka nie została rozpoczęta.")); }
continue;
}
if (Wybrano(sAction, sSend)) {
if (sSaved.Length == 0) {
try { sSaved = ProblemReportService.SaveAtomically(Path.Combine(App.DataDir, "Reports"), snapshot); }
catch { Dialog.Show(sTitle, T("Unable to save the report copy. Sending was not started.", "Nie udało się zapisać kopii zgłoszenia. Wysyłka nie została rozpoczęta.")); continue; }
}
if (sEndpoint.Length == 0) { Dialog.Show(T("Report a problem", "Zgłoś problem"), T("There is no active receiving endpoint. The redacted copy remains saved locally.", "Brak aktywnego punktu odbiorczego. Zredagowana kopia pozostała zapisana lokalnie.")); return false; }
if (!ProblemReportSendGate.TryBegin()) { Dialog.Show(T("Report a problem", "Zgłoś problem"), T("Another report is already being sent.", "Wysyłanie innego zgłoszenia już trwa.")); continue; }
ThreadPool.QueueUserWorkItem(delegate(object state) {
try {
ProblemReportSendResult result = ProblemReportService.Send(sEndpoint, snapshot, new ProblemReportHttpTransport());
if (frame == null || frame.IsDisposed) return;
frame.BeginInvoke((MethodInvoker)delegate {
if (result.Success) Dialog.Show(T("Report a problem", "Zgłoś problem"), T("The report was sent. Number: ", "Zgłoszenie wysłano. Numer: ") + result.IssueNumber.ToString() + ".");
else if (result.Ambiguous) Dialog.Show(T("Report a problem", "Zgłoś problem"), T("There is no definite confirmation of sending. It was not sent again automatically; the local copy remains preserved.", "Nie ma pewnego potwierdzenia wysyłki. Nie wysłano ponownie automatycznie; lokalna kopia pozostała zachowana."));
else if (result.Code == ProblemReportSendCode.Rejected) Dialog.Show(T("Report a problem", "Zgłoś problem"), T("The receiver declined the report. The local copy remains preserved.", "Odbiorca odmówił przyjęcia zgłoszenia. Lokalna kopia pozostała zachowana."));
else Dialog.Show(T("Report a problem", "Zgłoś problem"), T("Unable to send the report. The local copy remains preserved.", "Nie udało się wysłać zgłoszenia. Lokalna kopia pozostała zachowana."));
});
}
catch {}
finally { ProblemReportSendGate.Complete(); }
});
Dialog.Show(T("Report a problem", "Zgłoś problem"), T("Sending is in progress in the background. A local copy was saved.", "Trwa wysyłanie w tle. Lokalna kopia została zapisana."));
return false;
}
}
}

private static bool SafeLogExists() {
try { return !string.IsNullOrEmpty(App.ErrorLog) && File.Exists(App.ErrorLog); }
catch { return false; }
}

private static string ReadSafeLogTail() {
try {
if (!SafeLogExists()) return "";
byte[] ab = File.ReadAllBytes(App.ErrorLog);
int iStart = Math.Max(0, ab.Length - 64 * 1024);
string s = Encoding.UTF8.GetString(ab, iStart, ab.Length - iStart);
return ProblemReportRedactor.Redact(s, "");
}
catch { return ""; }
}
}