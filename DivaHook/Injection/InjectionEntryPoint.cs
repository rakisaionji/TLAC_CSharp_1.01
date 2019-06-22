using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyHook;
using DivaHook.Emulator;
using static DivaHook.Injection.InjectionHelper;

namespace DivaHook.Injection
{
    public sealed class InjectionEntryPoint : IEntryPoint
    {
        private const long ENGINE_UPDATE_HOOK_TARGET_ADDRESS = 0x000000014005D440L;

        private const long GLUT_SET_CURSOR_ADDRESS = 0x000000014073261CL;

        private InputEmulator emulator = null;

        private ServerInterface server = null;

        private Queue<string> messageQueue = new Queue<string>();

        private Stopwatch performanceWatch = new Stopwatch();

        private int performanceCounter = 0;

        private bool checkSetCursor = true;

        internal static void ShowCursor() => GlutSetCursor(GlutCursor.GLUT_CURSOR_RIGHT_ARROW);

        internal static void HideCursor() => GlutSetCursor(GlutCursor.GLUT_CURSOR_NONE);

        public InjectionEntryPoint(RemoteHooking.IContext context, string channelName)
        {
            // Connect to server object using provided channel name
            server = RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            server.Ping();
        }

        public void Run(RemoteHooking.IContext context, string channelName)
        {
            server.IsInstalled(RemoteHooking.GetCurrentProcessId());

            List<LocalHook> hooks = new List<LocalHook>()
            {
                LocalHook.Create(
                    new IntPtr(ENGINE_UPDATE_HOOK_TARGET_ADDRESS),
                    new VoidDelegate(PollInputOverride),
                    this),
            };

            foreach (var hook in hooks)
            {
                hook.ThreadACL.SetExclusiveACL(new int[] { 0 });
            }

            InputEmulator.KeyConfig.TryLoadConfig();
            server.ReportString($"DivaHook successfully established\n");
            server.ReportString($"Do not close this application...");

            RemoteHooking.WakeUpProcess();

            try
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(500);

                    string[] queued = null;

                    lock (messageQueue)
                    {
                        queued = messageQueue.ToArray();
                        messageQueue.Clear();
                    }

                    if (queued != null && queued.Length > 0)
                    {
                        server.ReportMessages(queued);
                    }
                    else
                    {
                        server.Ping();
                    }
                }
            }
            catch (Exception ex)
            {
                server.ReportException(ex);
            }

            foreach (var hook in hooks)
            {
                hook.Dispose();
            }

            LocalHook.Release();
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void VoidDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void GlutSetCursorDelegate(GlutCursor cursor);

        private static readonly GlutSetCursorDelegate GlutSetCursor = GetDelegateForFunctionPointer<GlutSetCursorDelegate>(GLUT_SET_CURSOR_ADDRESS);

        #region Overrides
        private void PollInputOverride()
        {
            performanceWatch.Restart();

            if (emulator == null)
            {
                emulator = new InputEmulator()
                {
                    LogToConsole = false,
                    CheckProcessActiveStopwatch = Stopwatch.StartNew(),
                };
            }

            if (emulator.CheckProcessActiveStopwatch.Elapsed >= InputEmulator.ProcessActiveCheckInterval)
            {
                emulator.IsDivaActive = emulator.MemoryManipulator.IsAttachedProcessActive();
                emulator.CheckProcessActiveStopwatch.Restart();
            }

            try
            {
                emulator.UpdateEmulatorInputTick(isProcessActive: emulator.IsDivaActive);
            }
            catch (Exception exception)
            {
                server.ReportException(exception);
                throw;
            }

            if (checkSetCursor)
            {
                ShowCursor();
                checkSetCursor = false;
            }

            performanceWatch.Stop();

            if (performanceCounter++ > 120)
            {
                //server.ReportMessage($"{performanceWatch.Elapsed.TotalMilliseconds}ms");
                performanceCounter = 0;
            }
        }
        #endregion
    }
}