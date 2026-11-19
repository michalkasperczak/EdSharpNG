// ControlledCommand.cs -- bounded, cancellable process runner for Prompt Command.
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace EdSharp {

public sealed class ControlledCommandResult {
public int ProcessId;
public int ExitCode;
public bool Cancelled;
public bool OutputTruncated;
public string StandardOutput = "";
public string StandardError = "";
} // ControlledCommandResult class

public sealed class ControlledCommand : IDisposable {
const uint CREATE_SUSPENDED = 0x00000004;
const uint CREATE_NO_WINDOW = 0x08000000;
const uint STARTF_USESTDHANDLES = 0x00000100;
const uint HANDLE_FLAG_INHERIT = 0x00000001;
const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
const int JobObjectExtendedLimitInformation = 9;
const uint INFINITE = 0xffffffff;
const int MaxCharactersPerStream = 131072;
IntPtr hJob;
IntPtr hProcess;
readonly object sync = new object();
bool cancelling;
bool completed;
Action<ControlledCommandResult> completedAction;

[StructLayout(LayoutKind.Sequential)]
struct SECURITY_ATTRIBUTES {
public int nLength;
public IntPtr lpSecurityDescriptor;
public bool bInheritHandle;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct STARTUPINFO {
public int cb;
public string lpReserved;
public string lpDesktop;
public string lpTitle;
public int dwX;
public int dwY;
public int dwXSize;
public int dwYSize;
public int dwXCountChars;
public int dwYCountChars;
public int dwFillAttribute;
public uint dwFlags;
public short wShowWindow;
public short cbReserved2;
public IntPtr lpReserved2;
public IntPtr hStdInput;
public IntPtr hStdOutput;
public IntPtr hStdError;
}

[StructLayout(LayoutKind.Sequential)]
struct PROCESS_INFORMATION {
public IntPtr hProcess;
public IntPtr hThread;
public int dwProcessId;
public int dwThreadId;
}

[StructLayout(LayoutKind.Sequential)]
struct JOBOBJECT_BASIC_LIMIT_INFORMATION {
public long PerProcessUserTimeLimit;
public long PerJobUserTimeLimit;
public uint LimitFlags;
public UIntPtr MinimumWorkingSetSize;
public UIntPtr MaximumWorkingSetSize;
public uint ActiveProcessLimit;
public UIntPtr Affinity;
public uint PriorityClass;
public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
struct IO_COUNTERS {
public ulong ReadOperationCount;
public ulong WriteOperationCount;
public ulong OtherOperationCount;
public ulong ReadTransferCount;
public ulong WriteTransferCount;
public ulong OtherTransferCount;
}

[StructLayout(LayoutKind.Sequential)]
struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
public IO_COUNTERS IoInfo;
public UIntPtr ProcessMemoryLimit;
public UIntPtr JobMemoryLimit;
public UIntPtr PeakProcessMemoryUsed;
public UIntPtr PeakJobMemoryUsed;
}

[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetInformationJobObject(IntPtr hJob, int infoClass, IntPtr info, int length);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool TerminateJobObject(IntPtr hJob, uint exitCode);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);
[DllImport("kernel32.dll", SetLastError = true)]
static extern uint ResumeThread(IntPtr hThread);
[DllImport("kernel32.dll", SetLastError = true)]
static extern uint WaitForSingleObject(IntPtr hHandle, uint milliseconds);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool GetExitCodeProcess(IntPtr hProcess, out uint exitCode);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool CloseHandle(IntPtr hObject);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref SECURITY_ATTRIBUTES attributes, int size);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern bool CreateProcess(string applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

sealed class OutputBuffer {
readonly StringBuilder text = new StringBuilder();
readonly object sync = new object();
bool truncated;
public void Add(char[] chars, int count) {
lock (sync) {
int room = MaxCharactersPerStream - text.Length;
if (room > 0) text.Append(chars, 0, Math.Min(room, count));
if (count > room && !truncated) {
truncated = true;
text.Append("\r\n[output truncated]\r\n");
}
}
}
public bool Truncated { get { lock (sync) return truncated; } }
public string Text { get { lock (sync) return text.ToString(); } }
}

ControlledCommand() {}

static string ErrorText() {
return new Win32Exception(Marshal.GetLastWin32Error()).Message;
}

static void Close(ref IntPtr handle) {
if (handle != IntPtr.Zero && handle != new IntPtr(-1)) CloseHandle(handle);
handle = IntPtr.Zero;
}

static bool MakePipe(out IntPtr readPipe, out IntPtr writePipe, bool childReads, out string error) {
readPipe = IntPtr.Zero;
writePipe = IntPtr.Zero;
error = "";
SECURITY_ATTRIBUTES attributes = new SECURITY_ATTRIBUTES();
attributes.nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES));
attributes.bInheritHandle = true;
if (!CreatePipe(out readPipe, out writePipe, ref attributes, 0)) { error = ErrorText(); return false; }
IntPtr parentEnd = childReads ? writePipe : readPipe;
if (!SetHandleInformation(parentEnd, HANDLE_FLAG_INHERIT, 0)) {
error = ErrorText(); Close(ref readPipe); Close(ref writePipe); return false;
}
return true;
}

static bool SetKillOnClose(IntPtr job, out string error) {
error = "";
JOBOBJECT_EXTENDED_LIMIT_INFORMATION limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
int size = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
IntPtr pointer = Marshal.AllocHGlobal(size);
try {
Marshal.StructureToPtr(limits, pointer, false);
if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, pointer, size)) { error = ErrorText(); return false; }
return true;
}
finally { Marshal.FreeHGlobal(pointer); }
}

public static bool TrySplitCommandLine(string commandLine, out string executable, out string arguments, out string error) {
executable = "";
arguments = "";
error = "";
string value = (commandLine ?? "").Trim();
if (value.Length == 0) { error = JezykInterfejsu.Wybierz("No command was entered.", "Nie wprowadzono polecenia."); return false; }
if (value.StartsWith("\"")) {
int quote = value.IndexOf('\"', 1);
if (quote <= 1) { error = JezykInterfejsu.Wybierz("The quoted program path is incomplete.", "Ujęta w cudzysłów ścieżka programu jest niepełna."); return false; }
executable = value.Substring(1, quote - 1);
arguments = value.Substring(quote + 1).Trim();
}
else {
int space = value.IndexOf(' ');
if (space < 0) executable = value;
else { executable = value.Substring(0, space); arguments = value.Substring(space + 1).Trim(); }
}
if (executable.Length == 0) { error = JezykInterfejsu.Wybierz("No program was entered.", "Nie wprowadzono programu."); return false; }
return true;
} // TrySplitCommandLine method

public static bool TryStart(string executable, string arguments, string workingDirectory, Action<ControlledCommandResult> completedAction, out ControlledCommand command, out string error) {
command = null;
error = "";
if (String.IsNullOrWhiteSpace(executable)) { error = JezykInterfejsu.Wybierz("No program was specified.", "Nie wskazano programu."); return false; }
if (!File.Exists(executable)) { error = JezykInterfejsu.Wybierz("The program file was not found.", "Nie znaleziono pliku programu."); return false; }
if (String.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory)) { error = JezykInterfejsu.Wybierz("The command working folder is unavailable.", "Katalog roboczy polecenia jest niedostępny."); return false; }
ControlledCommand current = new ControlledCommand();
current.completedAction = completedAction;
IntPtr inRead = IntPtr.Zero, inWrite = IntPtr.Zero, outRead = IntPtr.Zero, outWrite = IntPtr.Zero, errRead = IntPtr.Zero, errWrite = IntPtr.Zero;
PROCESS_INFORMATION processInformation = new PROCESS_INFORMATION();
try {
current.hJob = CreateJobObject(IntPtr.Zero, null);
if (current.hJob == IntPtr.Zero) { error = JezykInterfejsu.Wybierz("Could not create process control: ", "Nie można utworzyć kontroli procesu: ") + ErrorText(); return false; }
string jobError;
if (!SetKillOnClose(current.hJob, out jobError)) { error = JezykInterfejsu.Wybierz("Could not configure process control: ", "Nie można skonfigurować kontroli procesu: ") + jobError; return false; }
string pipeError;
if (!MakePipe(out inRead, out inWrite, true, out pipeError) || !MakePipe(out outRead, out outWrite, false, out pipeError) || !MakePipe(out errRead, out errWrite, false, out pipeError)) {
error = JezykInterfejsu.Wybierz("Could not create command pipes: ", "Nie można utworzyć potoków polecenia: ") + pipeError; return false;
}
STARTUPINFO startup = new STARTUPINFO();
startup.cb = Marshal.SizeOf(typeof(STARTUPINFO));
startup.dwFlags = STARTF_USESTDHANDLES;
startup.hStdInput = inRead;
startup.hStdOutput = outWrite;
startup.hStdError = errWrite;
StringBuilder commandLine = new StringBuilder("\"" + executable + "\"" + (String.IsNullOrEmpty(arguments) ? "" : " " + arguments));
if (!CreateProcess(executable, commandLine, IntPtr.Zero, IntPtr.Zero, true, CREATE_SUSPENDED | CREATE_NO_WINDOW, IntPtr.Zero, workingDirectory, ref startup, out processInformation)) {
error = JezykInterfejsu.Wybierz("Could not start the command: ", "Nie można uruchomić polecenia: ") + ErrorText(); return false;
}
current.hProcess = processInformation.hProcess;
Close(ref inRead); Close(ref outWrite); Close(ref errWrite); Close(ref inWrite); // input is deliberately closed; no interactive stdin.
if (!AssignProcessToJobObject(current.hJob, current.hProcess)) {
error = JezykInterfejsu.Wybierz("Could not control the command process: ", "Nie można sterować procesem polecenia: ") + ErrorText();
TerminateProcess(current.hProcess, 1);
return false;
}
if (ResumeThread(processInformation.hThread) == 0xffffffff) {
error = JezykInterfejsu.Wybierz("Could not resume the controlled command: ", "Nie można wznowić sterowanego polecenia: ") + ErrorText();
TerminateJobObject(current.hJob, 1);
return false;
}
Close(ref processInformation.hThread);
OutputBuffer standardOutput = new OutputBuffer();
OutputBuffer standardError = new OutputBuffer();
IntPtr outputReader = outRead; outRead = IntPtr.Zero;
IntPtr errorReader = errRead; errRead = IntPtr.Zero;
Task outTask = Task.Factory.StartNew(delegate { ReadPipe(outputReader, standardOutput); });
Task errTask = Task.Factory.StartNew(delegate { ReadPipe(errorReader, standardError); });
Task.Factory.StartNew(delegate { current.WaitForCompletion(outTask, errTask, standardOutput, standardError, processInformation.dwProcessId); });
command = current;
return true;
}
catch (Exception ex) {
error = JezykInterfejsu.Wybierz("Could not start the command: ", "Nie można uruchomić polecenia: ") + ex.Message;
return false;
}
finally {
Close(ref inRead); Close(ref inWrite); Close(ref outRead); Close(ref outWrite); Close(ref errRead); Close(ref errWrite); Close(ref processInformation.hThread);
if (command == null) {
if (processInformation.hProcess != IntPtr.Zero) { TerminateProcess(processInformation.hProcess, 1); Close(ref processInformation.hProcess); }
if (current.hJob != IntPtr.Zero) { Close(ref current.hJob); }
}
}
} // TryStart method

static void ReadPipe(IntPtr handle, OutputBuffer buffer) {
if (handle == IntPtr.Zero) return;
try {
using (FileStream stream = new FileStream(new SafeFileHandle(handle, true), FileAccess.Read, 4096, false))
using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, false), true, 4096)) {
char[] chars = new char[4096];
int count;
while ((count = reader.Read(chars, 0, chars.Length)) > 0) buffer.Add(chars, count);
}
}
catch { }
} // ReadPipe method

void WaitForCompletion(Task outTask, Task errTask, OutputBuffer standardOutput, OutputBuffer standardError, int processId) {
WaitForSingleObject(hProcess, INFINITE);
try { Task.WaitAll(new Task[] {outTask, errTask}); } catch { }
uint exitCode = 0;
GetExitCodeProcess(hProcess, out exitCode);
ControlledCommandResult result = new ControlledCommandResult();
result.ProcessId = processId;
result.ExitCode = unchecked((int) exitCode);
lock (sync) { result.Cancelled = cancelling; completed = true; }
result.StandardOutput = standardOutput.Text;
result.StandardError = standardError.Text;
result.OutputTruncated = standardOutput.Truncated || standardError.Truncated;
Close(ref hProcess);
Close(ref hJob);
Action<ControlledCommandResult> action = completedAction;
if (action != null) action(result);
} // WaitForCompletion method

public void Cancel() {
lock (sync) {
if (completed || cancelling) return;
cancelling = true;
if (hJob != IntPtr.Zero) TerminateJobObject(hJob, 1);
}
} // Cancel method

public void Dispose() { Cancel(); }
} // ControlledCommand class

} // namespace EdSharp
