using System;
using System.Reflection;
class ChecklistSafety {
static int pass,fail;
static Type tasks;
static void Check(string input,string expected){string actual=(string)tasks.GetMethod("ZdejmijPoleLuzne").Invoke(null,new object[]{input}); if(actual==expected){pass++;Console.WriteLine("PASS "+input);}else{fail++;Console.WriteLine("FAIL "+input+" -> "+actual);}}
static int Main(string[] args){tasks=Assembly.LoadFrom(args[0]).GetType("EdSharp.Zadania");
Check("- [a](https://example.org)","- [a](https://example.org)");
Check("- [x](https://example.org)","- [x](https://example.org)");
Check("- [x][target]","- [x][target]");
Check("- [x] (https://example.org)","- [x] (https://example.org)");
Check("- [x] [target]","- [x] [target]");
Check("- [a] ordinary text","- [a] ordinary text");
Check("- [] ordinary text","- [] ordinary text");
Check("- [x]: note","- [x]: note");
Check("- [ ]chleb","chleb");
Check("- [-] chleb","chleb");
Check("1. [ ] chleb","chleb");
Check("  - [X] chleb\r","  chleb\r");
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
}}
