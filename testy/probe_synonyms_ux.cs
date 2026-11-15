// Private UI harness: references the unchanged built EdSharpNG.exe.
// No production profile, registry writes, network calls or reader restarts.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using EdSharp;
using Homer;

class SynonymsUxProbe {
[STAThread] static void Main() {
Application.EnableVisualStyles();
string root=AppDomain.CurrentDomain.BaseDirectory;
App.ProgramName="EdSharpNG"; App.ProgramDir=root; App.DataDir=Path.Combine(root,"UxProfile");
Directory.CreateDirectory(App.DataDir);
App.NetDir=RuntimeEnvironment.GetRuntimeDirectory(); App.DefaultIniFile=Path.Combine(root,"EdSharp.ini");
App.IniFile=Path.Combine(App.DataDir,"test.ini"); App.HotkeyIniFile=Path.Combine(root,"Hotkeys.ini");
App.TempFile=Path.Combine(App.DataDir,"test.tmp"); App.IndentModeFile=Path.Combine(App.DataDir,"indent.tmp");
App.SpeechLog=Path.Combine(App.DataDir,"speech.log"); App.ErrorLog=Path.Combine(App.DataDir,"diagnostic.log"); App.ExtraSpeech=true;
App.Frame=new MdiFrame(); Homer.Say.attach(App.Frame);
App.Frame.Text="EdSharp Synonyms UX QA - private";
var child=new MdiChild(App.Frame,"Synonyms UX QA"); child.RTB.Text="dom"; child.RTB.Select(0,3);
var timer=new System.Windows.Forms.Timer(); timer.Interval=200;
timer.Tick+=delegate {
string command=Path.Combine(App.DataDir,"command.txt");
if(File.Exists(command)) {
string text=File.ReadAllText(command,Encoding.UTF8); File.Delete(command);
if(text.StartsWith("word\t",StringComparison.Ordinal) && App.Frame.OwnedForms.Length==0) {
child.RTB.Text=text.Substring(5); child.RTB.Modified=false; child.RTB.Select(0,child.RTB.TextLength); App.Frame.Activate();child.RTB.Focus();
}
}
var forms=new List<object>();
foreach(Form form in Application.OpenForms) {
var controls=new List<object>();
Snapshot(form,controls);
forms.Add(new {title=form.Text, active=form.ContainsFocus, controls=controls});
}
var state=new {pid=System.Diagnostics.Process.GetCurrentProcess().Id, version=App.VersionString,
 document=child.RTB.Text, selectionStart=child.RTB.SelectionStart, selectionLength=child.RTB.SelectionLength,
 dictionary=App.ReadOption("SynonymDictionary","pl"), forms=forms};
string path=Path.Combine(App.DataDir,"state.json");
File.WriteAllText(path+".new",new JavaScriptSerializer().Serialize(state),new UTF8Encoding(false));
if(File.Exists(path)) File.Delete(path); File.Move(path+".new",path);
};
timer.Start(); App.Frame.Shown+=delegate {child.RTB.Focus();};
Application.Run(App.Frame);
}
static void Snapshot(Control owner,List<object> result) {
foreach(Control c in owner.Controls) {
if(c is ListBox) {var l=(ListBox)c;result.Add(new {type="list",name=c.AccessibleName,index=l.SelectedIndex,focus=l.Focused,items=l.Items.Cast<object>().Select(x=>x.ToString()).ToArray()});}
else if(c is TextBox && !(c is HomerRichTextBox)) {var t=(TextBox)c;result.Add(new {type="text",name=c.AccessibleName,text=t.Text,readOnly=t.ReadOnly,focus=t.Focused});}
else if(c is Button) result.Add(new {type="button",name=c.AccessibleName,text=c.Text,focus=c.Focused});
else if(c is ComboBox) {var t=(ComboBox)c;result.Add(new {type="combo",name=c.AccessibleName,text=t.Text,index=t.SelectedIndex,focus=t.Focused,items=t.Items.Cast<object>().Select(x=>x.ToString()).ToArray()});}
Snapshot(c,result);
}
}
}
