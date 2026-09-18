// Run against the actual built executable in an STA Windows process.
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
class RichCopyProbe {
static Type frame; static int pass, fail;
static object Call(string name, params object[] args) {return frame.GetMethod(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);}
static void Check(bool b,string name) {Console.WriteLine((b ? "PASS " : "FAIL ")+name); if(b)pass++;else fail++;}
static DataObject Make(string text) {return (DataObject)Call("BuildMarkdownSelectionClipboardData",text);}
[STAThread] static int Main(string[] args) {
try {
string exe=Path.GetFullPath(args[0]); frame=Assembly.LoadFrom(exe).GetType("EdSharp.MdiFrame");
string source=File.ReadAllText(args[1]); DataObject d=Make(source);
string rtf=(string)d.GetData(DataFormats.Rtf), html=(string)d.GetData(DataFormats.Html);
Check((string)d.GetData("EdSharpNG.Markdown")==source,"exact Markdown roundtrip");
Check(rtf.Contains(@"\pard\plain\s2\outlinelevel0") && rtf.Contains(@"\pard\plain\s3\outlinelevel1"),"heading style applied to paragraphs, not just declared");
Check(rtf.Contains("HYPERLINK \"https://example.org/test?q=1&x=2\""),"RTF actual hyperlink");
using(RichTextBox box=new RichTextBox()) {box.Rtf=rtf; Check(box.Text.Contains("Żółty nagłówek") && box.Text.Contains("Koniec dokumentu."),"RTF parsed by native RichEdit without text loss");}
Check(html.Contains("<h1>") && html.Contains("<h2>") && html.Contains("<ul>"),"HTML headings and list");
byte[] b=Encoding.UTF8.GetBytes(html); int start=Int32.Parse(Regex.Match(html,@"StartFragment:(\d+)").Groups[1].Value), end=Int32.Parse(Regex.Match(html,@"EndFragment:(\d+)").Groups[1].Value);
string frag=Encoding.UTF8.GetString(b,start,end-start);
Check(System.Net.WebUtility.HtmlDecode(frag).Contains("Żółty nagłówek") && frag.Contains("łączem") && Encoding.UTF8.GetString(b,end,18)=="<!--EndFragment-->","UTF-8 byte offsets");
File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe),"rich-after.rtf"),rtf,Encoding.ASCII);
File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe),"rich-after.html"),"<!doctype html><html><meta charset=\"utf-8\"><body>"+frag+"</body></html>",new UTF8Encoding(false));
DataObject auto=Make("Address <https://example.org/auto> and https://example.org/bare.");
Check(((string)auto.GetData(DataFormats.Html)).Contains("href=\"https://example.org/auto\""),"HTML automatic link");
Check(((string)auto.GetData(DataFormats.Rtf)).Contains("HYPERLINK \"https://example.org/bare\""),"RTF bare URL without final punctuation");
DataObject literal=Make("Code `https://example.org/literal` and [real](https://example.org/real).");
Check(!((string)literal.GetData(DataFormats.Html)).Contains("href=\"https://example.org/literal\""),"inline code is not a hyperlink");
Check(!((string)literal.GetData(DataFormats.Rtf)).Contains("HYPERLINK \"https://example.org/literal\""),"inline code is not a RTF field");
DataObject one=Make("[link](https://example.org/one)");
Check(((string)one.GetData(DataFormats.Html)).Contains("href=\"https://example.org/one\""),"single-line selection has HTML link");
DataObject part=Make("sele");
Check((string)part.GetData(DataFormats.UnicodeText)=="sele" && (string)part.GetData("EdSharpNG.Markdown")=="sele","partial line does not expand to item");
Clipboard.SetDataObject(d,true,10,80);
IDataObject back=Clipboard.GetDataObject();
Check(back.GetDataPresent(DataFormats.Html) && back.GetDataPresent(DataFormats.Rtf),"both rich flavors read back from Windows clipboard");
using(Form form=new Form()) using(WebBrowser web=new WebBrowser()) {
form.Controls.Add(web); web.Dock=DockStyle.Fill; form.Show();
web.DocumentText="<html><body><div id='target' contenteditable='true'></div></body></html>";
DateTime until=DateTime.UtcNow.AddSeconds(15);
while(web.ReadyState!=WebBrowserReadyState.Complete && DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}
if(web.Document==null)throw new Exception("HTML consumer did not load");
web.Document.Body.SetAttribute("contentEditable","true"); web.Document.Body.Focus();
web.Document.ExecCommand("Paste",false,null); Application.DoEvents();
string pasted=web.Document.Body.InnerHtml ?? "";
Check(Regex.IsMatch(pasted,"<H1[ >]",RegexOptions.IgnoreCase),"independent MSHTML paste retains heading");
Check(pasted.Contains("https://example.org/test?q=1"),"independent MSHTML paste retains link destination");
Check(Regex.IsMatch(pasted,"<UL[ >]",RegexOptions.IgnoreCase),"independent MSHTML paste retains list");
File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe),"rich-pasted-mshtml.html"),pasted,new UTF8Encoding(false));
form.Close(); }
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL"); return fail==0 ? 0:1;
} catch(Exception e) {Console.WriteLine(e); return 2;}
}
}
