using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
class ActualEnglish {
static int failures;
static void Check(bool ok,string text){Console.WriteLine((ok?"PASS ":"FAIL ")+text);if(!ok)failures++;}
static int Count(List<Synonimy.Grupa> groups){int n=0;foreach(var g in groups)n+=g.Synonyms.Count;return n;}
static int Main(string[] args){Console.OutputEncoding=new UTF8Encoding(false);string folder=args[0];var times=new List<long>();int words=0,hits=0;foreach(string word in File.ReadAllLines(args[1])){if(word.Length==0||word.StartsWith("#"))continue;var t=Stopwatch.StartNew();var groups=SlownikiSynonimow.Find(folder,word,"en");t.Stop();times.Add(t.ElapsedMilliseconds);int count=Count(groups);words++;if(count>0)hits++;Console.WriteLine("WORD\t"+word+"\t"+groups.Count+"\t"+count+"\t"+t.ElapsedMilliseconds);if(SlownikiSynonimow.Error.Length>0){Console.WriteLine(SlownikiSynonimow.Error);failures++;}}
Check(words>=100,"at least100 distinct explicit common English inputs");
foreach(string word in new string[]{"bank","list","light","help","go","happy","good","NASA","children","went","ablaze"}){var groups=SlownikiSynonimow.Find(folder,word,"en");Check(groups.Count>0,"actual English data "+word);foreach(var g in groups){Check(g.SenseId.StartsWith("wn30:"+g.PartOfSpeech+":"),"source POS isolated "+word+" "+g.SenseId);Check(SlownikiSynonimow.GetDetails(g.SenseId).Contains("Princeton WordNet 3.0"),"source details exist "+g.SenseId);}}
var both=SlownikiSynonimow.Find(folder,"list","both");bool pl=false,en=false;foreach(var g in both){if(g.SenseId.StartsWith("wn30:"))en=true;else pl=true;}Check(pl&&en,"actual list has separated Polish and English senses");
var polish=SlownikiSynonimow.Find(folder,"domami","pl");bool inflected=false;foreach(var g in polish)if(g.Synonyms.Contains("chałupami"))inflected=true;Check(inflected,"actual Polish inflection domami to chałupami preserved");
times.Sort();Console.WriteLine("WORDS="+words+" HITS="+hits+" MEDIAN_MS="+times[times.Count/2]+" MAX_MS="+times[times.Count-1]);Console.WriteLine("PEAK_BYTES="+Process.GetCurrentProcess().PeakWorkingSet64);Console.WriteLine("FAILURES="+failures);return failures==0?0:1;}
}
