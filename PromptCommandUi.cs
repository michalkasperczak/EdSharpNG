// PromptCommandUi.cs -- explicit save decision and readable result window for Alt+F5.
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EdSharp {

public enum PromptCommandSaveChoice { Cancel, SaveAndRun, UseDiskVersion }

public static class PromptCommandSaveDialog {
public static PromptCommandSaveChoice Ask(Form owner, bool hasDiskFile) {
using (Form form = new Form()) {
form.Text = "Run Command";
form.StartPosition = FormStartPosition.CenterParent;
form.MinimizeBox = false;
form.MaximizeBox = false;
form.ShowInTaskbar = false;
form.Width = 620;
form.Height = 250;
form.KeyPreview = true;
TextBox message = new TextBox();
message.Multiline = true;
message.ReadOnly = true;
message.BorderStyle = BorderStyle.None;
message.TabStop = true;
message.AccessibleName = "Save decision";
message.Text = hasDiskFile
? JezykInterfejsu.Wybierz("This command uses %Source% or %SourceDir%, and the document has changes. Save and run writes the current document first. Use last disk version runs with the existing disk file. Cancel does not start a command.", "To polecenie używa %Source% lub %SourceDir%, a dokument ma zmiany. Zapisz i uruchom najpierw zapisuje bieżący dokument. Użyj ostatniej wersji z dysku uruchamia polecenie na istniejącym pliku. Anuluj nie uruchamia polecenia.")
: JezykInterfejsu.Wybierz("This command uses %Source% or %SourceDir%, and this document has no disk file. Save and run opens Save As first. Cancel does not start a command.", "To polecenie używa %Source% lub %SourceDir%, a dokument nie ma pliku na dysku. Zapisz i uruchom najpierw otwiera Zapisz jako. Anuluj nie uruchamia polecenia.");
message.SetBounds(14, 14, 575, 95);
Button save = new Button(); save.Text = "Save and run"; save.AutoSize = true; save.SetBounds(14, 130, 130, 32);
Button disk = new Button(); disk.Text = "Use last disk version"; disk.AutoSize = true; disk.SetBounds(155, 130, 155, 32); disk.Enabled = hasDiskFile;
Button cancel = new Button(); cancel.Text = "Cancel"; cancel.AutoSize = true; cancel.SetBounds(321, 130, 90, 32);
PromptCommandSaveChoice choice = PromptCommandSaveChoice.Cancel;
save.Click += delegate { choice = PromptCommandSaveChoice.SaveAndRun; form.Close(); };
disk.Click += delegate { choice = PromptCommandSaveChoice.UseDiskVersion; form.Close(); };
cancel.Click += delegate { form.Close(); };
form.Controls.AddRange(new Control[] {message, save, disk, cancel});
form.CancelButton = cancel;
form.Shown += delegate { message.Focus(); message.Select(0, 0); };
form.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; form.Close(); } };
form.ShowDialog(owner);
return choice;
}
} // Ask method
} // PromptCommandSaveDialog class

public sealed class PromptCommandResultForm : Form {
readonly TextBox resultBox;
readonly Button cancel;
readonly Label state;
ControlledCommand command;
bool finished;

public PromptCommandResultForm() {
Text = "Command Result";
Width = 800;
Height = 560;
StartPosition = FormStartPosition.CenterParent;
KeyPreview = true;
resultBox = new TextBox();
resultBox.Multiline = true;
resultBox.ReadOnly = true;
resultBox.ScrollBars = ScrollBars.Both;
resultBox.WordWrap = false;
resultBox.AccessibleName = "Command result";
resultBox.SetBounds(12, 12, 760, 430);
state = new Label();
state.Text = "Running. Standard input is closed. Escape or Cancel ends this command.";
state.SetBounds(12, 452, 650, 28);
cancel = new Button();
cancel.Text = "Cancel";
cancel.AutoSize = true;
cancel.Enabled = false;
cancel.SetBounds(680, 447, 90, 32);
cancel.Click += delegate { CancelCommand(); };
Controls.AddRange(new Control[] {resultBox, state, cancel});
CancelButton = cancel;
FormClosing += delegate(object sender, FormClosingEventArgs e) {
if (!finished && command != null) { e.Cancel = true; CancelCommand(); }
};
Shown += delegate { resultBox.Focus(); };
}

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
if (keyData == (Keys.Control | Keys.A) && resultBox.Focused) {
resultBox.SelectAll();
return true;
}
if (keyData == Keys.Escape) {
if (finished) Close(); else CancelCommand();
return true;
}
return base.ProcessCmdKey(ref msg, keyData);
}

public void Attach(ControlledCommand value) { command = value; cancel.Enabled = true; }

void CancelCommand() {
if (finished || command == null) return;
cancel.Enabled = false;
state.Text = "Cancelling the controlled command...";
command.Cancel();
}

public void Complete(ControlledCommandResult result) {
if (IsDisposed) return;
finished = true;
cancel.Enabled = false;
string status = result.Cancelled ? "Cancelled." : "Exit code: " + result.ExitCode + ".";
state.Text = status + " Use Control+A and Control+C to select and copy the result.";
resultBox.Text = "Status: " + status + "\r\n\r\nStandard output:\r\n" + result.StandardOutput + "\r\n\r\nStandard error:\r\n" + result.StandardError;
resultBox.Select(0, 0);
resultBox.Focus();
}
} // PromptCommandResultForm class

} // namespace EdSharp
