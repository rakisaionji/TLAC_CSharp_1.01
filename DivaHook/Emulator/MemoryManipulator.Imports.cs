using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator
{
    public partial class MemoryManipulator
    {
        private const string USER32_DLL = "user32.dll";

        private const string KERNEL32_DLL = "kernel32.dll";

        [DllImport(USER32_DLL)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(USER32_DLL)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rectangle);

        [DllImport(USER32_DLL)]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT rectangle);

        [DllImport(USER32_DLL)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport(KERNEL32_DLL)]
        public static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport(KERNEL32_DLL)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport(KERNEL32_DLL)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport(KERNEL32_DLL)]
        public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport(KERNEL32_DLL)]
        public static extern bool WriteProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
    }
}
