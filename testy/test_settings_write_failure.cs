// Real WinForms Settings and Win32 INI writes, isolated profile only.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EdSharp;
using Homer;
class SettingsWriteFailureTests {
static IEnumerable<Control> Controls(Control c) {foreach(Control x in c.Controls){yield return x;foreach(var y in Controls(x))yield return y;}}
[STAThread] static int Main(string[] args) {
Console.OutputEncoding=new System.Text.UTF8Encoding(false);
bool restore=args.Contains("restore");
bool writable=args.Contains("success");
Application.EnableVisualStyles();
string root=AppDomain.CurrentDomain.BaseDirectory, profile=Path.Combine(root,"settings-write-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(profile);
try {
App.ProgramName="EdSharpNG";App.ProgramDir=root;App.DataDir=profile;App.NetDir=RuntimeEnvironment.GetRuntimeDirectory();
App.DefaultIniFile=Path.Combine(root,"EdSharp.ini");App.IniFile=Path.Combine(profile,"test.ini");App.HotkeyIniFile=Path.Combine(root,"Hotkeys.ini");
App.TempFile=Path.Combine(profile,"output.txt");App.IndentModeFile=Path.Combine(profile,"indent.tmp");App.SpeechLog=Path.Combine(profile,"speech.log");App.ErrorLog=Path.Combine(profile,"error.log");App.CaptureOutput=true;
string original=restore?"en":"pl";
if(!App.WriteOption("SynonymDictionary",original))throw new Exception("Fixture must be writable initially");
if(!writable) {
File.SetAttributes(App.IniFile,FileAttributes.ReadOnly);
if(App.WriteOption("SynonymDictionary",restore?"pl":"en"))throw new Exception("Calibration: read-only INI unexpectedly writable");
}
App.Frame=new MdiFrame();Say.attach(App.Frame);var child=new MdiChild(App.Frame,"Private settings write test");App.Frame.Show();
var timer=new Timer();timer.Interval=100;bool selected=false;
var confirmTimer=new Timer();confirmTimer.Interval=100;
confirmTimer.Tick+=delegate {
foreach(var f in Application.OpenForms.Cast<Form>().ToArray()) {
var yes=Controls(f).OfType<Button>().FirstOrDefault(b=>b.Text.Replace("&","")=="Yes");
if(yes!=null){confirmTimer.Stop();yes.PerformClick();return;}
}
};
timer.Tick+=delegate {
var forms=Application.OpenForms.Cast<Form>().Where(f=>f!=App.Frame && f!=child).ToArray();
foreach(var f in forms) {
var combo=Controls(f).OfType<ComboBox>().FirstOrDefault(c=>c.Items.Cast<object>().Any(x=>x.ToString()=="Angielski") && c.Items.Cast<object>().Any(x=>x.ToString()=="Polski"));
if(combo==null)continue;
combo.SelectedIndex=combo.Items.IndexOf("Angielski");selected=true;timer.Stop();
if(restore)confirmTimer.Start();
Controls(f).OfType<Button>().First(b=>b.Text.Replace("&","")==(restore?"Restore defaults":"OK")).PerformClick();return;
}
};
timer.Start();App.Frame.PokazUstawienia("SynonymDictionary");timer.Dispose();confirmTimer.Dispose();
string value=App.ReadOption("SynonymDictionary","pl"), message=File.Exists(App.TempFile)?File.ReadAllText(App.TempFile):"";
Console.WriteLine("READ_ONLY="+!writable+";SELECTED="+selected+";VALUE="+value);Console.WriteLine("MESSAGE="+message);
bool ok=writable
 ? selected && value==(restore?"pl":"en") && !message.Contains("Nie zapisano") && message.Contains(restore?"settings restored to defaults":"One setting saved")
 : selected && value==original && message.Contains("Nie zapisano") && !message.Contains("One setting saved") && !message.Contains("settings restored to defaults");
child.RTB.Modified=false;App.Frame.Close();Console.WriteLine(ok?"PASS truthful settings outcome":"FAIL: settings outcome must match the actual INI write result");return ok?0:1;
} finally {if(File.Exists(App.IniFile))File.SetAttributes(App.IniFile,FileAttributes.Normal);Directory.Delete(profile,true);}
}
}
