using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Toolkids.Models;

namespace Toolkids.Services.Sandbox
{
    /// <summary>按 <see cref="WaitMode"/> 等待目标软件退出。Tree 用 Job Object 跟踪整棵进程树。</summary>
    public static class ProcessWaiter
    {
        public static void Wait(Process proc, WaitMode mode, string? namedProcess)
        {
            switch (mode)
            {
                case WaitMode.None: return;
                case WaitMode.Process: proc.WaitForExit(); return;
                case WaitMode.Named: WaitNamed(string.IsNullOrWhiteSpace(namedProcess) ? proc.ProcessName : namedProcess!); return;
                default: WaitTree(proc); return; // Tree
            }
        }

        private static void WaitNamed(string name)
        {
            string n = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
            while (Process.GetProcessesByName(n).Length > 0)
                Thread.Sleep(700);
        }

        // 把进程放进 Job Object，轮询活动进程数直到为 0（覆盖它拉起的子进程）。
        private static void WaitTree(Process proc)
        {
            IntPtr job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) { proc.WaitForExit(); return; }
            try
            {
                IntPtr h = OpenProcess(PROCESS_SET_QUOTA | PROCESS_TERMINATE, false, proc.Id);
                if (h == IntPtr.Zero || !AssignProcessToJobObject(job, h))
                {
                    if (h != IntPtr.Zero) CloseHandle(h);
                    proc.WaitForExit(); // 老系统/已在其它 job 中时退化为只等主进程
                    return;
                }
                CloseHandle(h);
                while (ActiveProcessCount(job) > 0)
                    Thread.Sleep(600);
            }
            finally { CloseHandle(job); }
        }

        private static int ActiveProcessCount(IntPtr job)
        {
            int size = Marshal.SizeOf<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                if (!QueryInformationJobObject(job, JobObjectBasicAccountingInformation, ptr, size, out _))
                    return 0;
                var info = Marshal.PtrToStructure<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(ptr);
                return (int)info.ActiveProcesses;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        // ---- P/Invoke ----
        private const uint PROCESS_TERMINATE = 0x0001;
        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const int JobObjectBasicAccountingInformation = 1;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? name);

        [DllImport("kernel32.dll")]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll")]
        private static extern bool QueryInformationJobObject(IntPtr job, int infoClass, IntPtr info, int length, out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr h);

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
        {
            public long TotalUserTime;
            public long TotalKernelTime;
            public long ThisPeriodTotalUserTime;
            public long ThisPeriodTotalKernelTime;
            public uint TotalPageFaultCount;
            public uint TotalProcesses;
            public uint ActiveProcesses;
            public uint TotalTerminatedProcesses;
        }
    }
}
