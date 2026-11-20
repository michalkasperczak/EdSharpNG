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
private static Button FindButton(Control parent, string text) { if (parent == null) return null; foreach (Control child in parent.Controls) { Button button = child as Button; if (button != null && string.Equals((button.Text ?? "").Replace("&", ""), text ?? "", StringComparison.OrdinalIgnoreCase)) return button; Button nested = FindButton(child, text); if (nested != null) return nested; } return null; }

public static void Show(MdiFrame frame, string sPreSubject, string sPreDescription) {
ProblemReportDraft draft = new ProblemReportDraft();
draft.Subject = sPreSubject ?? "";
draft.Description = sPreDescription ?? "";
draft.IncludeDiagnosticLog = Ustawienia.CzyWlaczone(App.ReadOption("AttachDiagnosticLog", "N"));
ProblemReportSnapshot previous = null;
string sChannel = ProblemReportService.ResolveChannel(App.ReadData("ProblemReportEndpointProfile", "public"));
bool bSending = false;
Form activeEditor = null;
Label activeStatus = null;
TextBox activeSubject = null, activeDescription = null, activeEmail = null;
CheckBox activeConsent = null, activeAttach = null;
Button activePreview = null, activeSend = null, activeCancel = null;
bool activeSafeLogAvailable = false;
Action<ProblemReportSendResult> completed = delegate(ProblemReportSendResult result) {
bSending = false;
Form completedEditor = activeEditor;
if (result.Success) {
if (completedEditor != null && !completedEditor.IsDisposed) completedEditor.Close();
if (frame != null && !frame.IsDisposed) frame.BeginInvoke((MethodInvoker)delegate { Dialog.Show(T("Report a problem", "Zgłoś problem"), T("The report was sent. Number: ", "Zgłoszenie wysłano. Numer: ") + result.IssueNumber.ToString() + "."); });
return;
}
if (completedEditor != null && !completedEditor.IsDisposed) {
if (activeStatus != null) activeStatus.Text = result.Ambiguous ? T("Sending ended without definite confirmation. The local copy remains preserved; you can edit the report before deciding what to do next.", "Wysyłka zakończyła się bez pewnego potwierdzenia. Lokalna kopia pozostała zachowana; możesz edytować zgłoszenie przed decyzją o dalszym kroku.") : result.Code == ProblemReportSendCode.Rejected ? T("The receiver declined the report. The local copy remains preserved; you can edit the report before deciding what to do next.", "Odbiorca odmówił przyjęcia zgłoszenia. Lokalna kopia pozostała zachowana; możesz edytować zgłoszenie przed decyzją o dalszym kroku.") : T("Unable to send the report. The local copy remains preserved; you can edit the report before deciding what to do next.", "Nie udało się wysłać zgłoszenia. Lokalna kopia pozostała zachowana; możesz edytować zgłoszenie przed decyzją o dalszym kroku.");
if (activeSubject != null) activeSubject.ReadOnly = false;
if (activeDescription != null) activeDescription.ReadOnly = false;
if (activeEmail != null) activeEmail.ReadOnly = false;
if (activeConsent != null) activeConsent.Enabled = true;
if (activeAttach != null) activeAttach.Enabled = activeSafeLogAvailable;
if (activePreview != null) activePreview.Enabled = true;
if (activeSend != null) activeSend.Enabled = true;
if (activeCancel != null) { activeCancel.Text = T("&Cancel", "&Anuluj"); activeCancel.AccessibleName = T("Cancel", "Anuluj"); }
}
if (result.Ambiguous) Dialog.Show(T("Report a problem", "Zgłoś problem"), T("There is no definite confirmation of sending. It was not sent again automatically; the local copy remains preserved.", "Nie ma pewnego potwierdzenia wysyłki. Nie wysłano ponownie automatycznie; lokalna kopia pozostała zachowana."));
else if (result.Code == ProblemReportSendCode.Rejected) Dialog.Show(T("Report a problem", "Zgłoś problem"), T("The receiver declined the report. The local copy remains preserved.", "Odbiorca odmówił przyjęcia zgłoszenia. Lokalna kopia pozostała zachowana."));
else Dialog.Show(T("Report a problem", "Zgłoś problem"), T("Unable to send the report. The local copy remains preserved.", "Nie udało się wysłać zgłoszenia. Lokalna kopia pozostała zachowana."));
if (completedEditor != null && !completedEditor.IsDisposed && activeDescription != null) activeDescription.Focus();
};
while (true) {
bool bSendingActive = ProblemReportService.ReadEndpoint(App.ProgramDir, sChannel).Length > 0;
string sTitle = T("Report a problem", "Zgłoś problem");
string sPreview = T("&Preview", "&Podgląd");
string sSend = T("Send &report", "Wyślij &zgłoszenie");
string sCancel = T("&Cancel", "&Anuluj");
LbcDialog edit = new LbcDialog(sTitle, frame);
Label status = edit.addLabel(bSending ? T("Sending is in progress. This form is read-only until the result is known. Closing the form does not cancel sending.", "Trwa wysyłanie. Ten formularz jest tylko do odczytu do czasu poznania wyniku. Zamknięcie formularza nie cofa wysyłki.") : bSendingActive ? T("Use Send report to deliver the redacted report now, or Preview to inspect it first. Sending is available only to a configured HTTPS endpoint.", "Użyj Wyślij zgłoszenie, aby od razu nadać zredagowany raport, albo Podgląd, aby go najpierw sprawdzić. Wysyłka jest możliwa tylko do skonfigurowanego punktu HTTPS.") : T("Sending is currently inactive. Send report remains available so you can receive the reason and preserve a local copy; it will not be sent.", "Wysyłka jest obecnie nieaktywna. Wyślij zgłoszenie pozostaje dostępne, aby podać powód i zachować kopię lokalną; zgłoszenie nie zostanie nadane."));
TextBox subject = edit.addInputBox(T("&Subject", "&Temat"), draft.Subject, T("A short description of the problem.", "Krótki opis problemu."));
TextBox description = edit.addTextMemo(T("&Description", "&Opis"), draft.Description);
TextBox email = edit.addInputBox(T("Optional &email address", "Opcjonalny adres &e-mail"), draft.ContactEmail, T("The address will be shared privately only with your explicit consent.", "Adres zostanie przekazany wyłącznie prywatnie po wyraźnej zgodzie."));
CheckBox consent = edit.addCheckBox(T("I consent to sharing my email address privately", "Wyrażam zgodę na przekazanie adresu e-mail prywatnie"), draft.ContactConsented,
T("Without this consent, the email address will not be included in the report.", "Bez tej zgody adres e-mail nie zostanie dołączony do zgłoszenia."));
bool bSafeLogAvailable = Ustawienia.CzyWlaczone(App.ReadOption("DiagnosticLogging", "Y")) && SafeLogExists();
CheckBox attach = edit.addCheckBox(T("Include a safe &diagnostic log", "Dołącz bezpieczny dziennik &diagnostyczny"), bSafeLogAvailable && draft.IncludeDiagnosticLog,
T("Only a redacted technical log is included; it does not include speech or document contents.", "Dołączany jest tylko zredagowany dziennik techniczny; nie obejmuje mowy ani treści dokumentów."));
if (!bSafeLogAvailable) { attach.Enabled = false; attach.Checked = false; }
bool bPendingEditor = bSending;
if (bPendingEditor) { subject.ReadOnly = true; description.ReadOnly = true; email.ReadOnly = true; consent.Enabled = false; attach.Enabled = false; }
edit.setInitialFocus(description);
activeEditor = edit.form;
activeStatus = status; activeSubject = subject; activeDescription = description; activeEmail = email; activeConsent = consent; activeAttach = attach; activeSafeLogAvailable = bSafeLogAvailable;
if (bPendingEditor) edit.form.Shown += delegate(object sender, EventArgs args) { activePreview = FindButton(edit.form, sPreview.Replace("&", "")); activeSend = FindButton(edit.form, sSend.Replace("&", "")); activeCancel = FindButton(edit.form, sCancel.Replace("&", "")); if (activePreview != null) activePreview.Enabled = false; if (activeSend != null) activeSend.Enabled = false; if (activeCancel != null) { activeCancel.Text = T("&Close form", "&Zamknij formularz"); activeCancel.AccessibleName = T("Close form", "Zamknij formularz"); } };
string sAction = edit.runWithButtons(new string[] { sPreview, sSend, sCancel });
draft.Subject = subject.Text; draft.Description = description.Text; draft.ContactEmail = email.Text; draft.ContactConsented = consent.Checked;
draft.IncludeDiagnosticLog = bSafeLogAvailable && attach.Checked; edit.Dispose();
activeEditor = null;
if (Wybrano(sAction, sCancel) || sAction.Length == 0) return;
try {
draft.DiagnosticLogText = draft.IncludeDiagnosticLog ? ReadSafeLogTail() : "";
ProblemReportTechnical technical = new ProblemReportTechnical(App.VersionString, Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit"), Environment.Version.ToString());
ProblemReportSnapshot snapshot = ProblemReportService.CreateSnapshot(draft, technical, previous, sChannel);
previous = snapshot;
if (Wybrano(sAction, sSend)) {
if (StartSend(frame, snapshot, completed)) { bSending = true; continue; }
continue;
}
bool bStartedFromPreview;
if (!Preview(frame, snapshot, completed, out bStartedFromPreview)) return;
if (bStartedFromPreview) bSending = true;
continue;
}
catch (ArgumentException ex) { Dialog.Show(sTitle, ex.Message); }
catch { Dialog.Show(sTitle, T("Unable to create a safe report preview.", "Nie udało się utworzyć bezpiecznego podglądu zgłoszenia.")); }
}
}

private static bool Preview(MdiFrame frame, ProblemReportSnapshot snapshot, Action<ProblemReportSendResult> completed, out bool bStarted) {
bStarted = false;
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
if (Wybrano(sAction, sSend)) { if (StartSend(frame, snapshot, completed)) { bStarted = true; return true; } continue; }
}
}

private static bool StartSend(MdiFrame frame, ProblemReportSnapshot snapshot, Action<ProblemReportSendResult> completed) {
string sTitle = T("Report a problem", "Zgłoś problem");
string sSaved;
try { sSaved = ProblemReportService.SaveAtomically(Path.Combine(App.DataDir, "Reports"), snapshot); }
catch { Dialog.Show(sTitle, T("Unable to save the report copy. Sending was not started.", "Nie udało się zapisać kopii zgłoszenia. Wysyłka nie została rozpoczęta.")); return false; }
string sEndpoint = ProblemReportService.ReadEndpoint(App.ProgramDir, snapshot.Channel);
if (sEndpoint.Length == 0) { Dialog.Show(sTitle, T("There is no active receiving endpoint. The redacted copy remains saved locally.\r\nPath: ", "Brak aktywnego punktu odbiorczego. Zredagowana kopia pozostała zapisana lokalnie.\r\nŚcieżka: ") + sSaved); return false; }
if (!ProblemReportSendGate.TryBegin()) { Dialog.Show(sTitle, T("Another report is already being sent. The local copy remains preserved.\r\nPath: ", "Wysyłanie innego zgłoszenia już trwa. Lokalna kopia pozostała zachowana.\r\nŚcieżka: ") + sSaved); return false; }
ThreadPool.QueueUserWorkItem(delegate(object state) {
bool bPosted = false;
try {
ProblemReportSendResult result = ProblemReportService.Send(sEndpoint, snapshot, new ProblemReportHttpTransport());
if (frame == null || frame.IsDisposed) return;
frame.BeginInvoke((MethodInvoker)delegate { try { if (completed != null) completed(result); } finally { ProblemReportSendGate.Complete(); } });
bPosted = true;
}
catch {}
finally { if (!bPosted) ProblemReportSendGate.Complete(); }
});
return true;
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
