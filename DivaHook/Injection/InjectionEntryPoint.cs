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
        private const long POLL_INPUT_FUNC_ADDRESS = 0x14018CC40L;

        private const long LOG_FUNC_ADDRESS = 0x1403F2620L;

        private const long LOG_GAME_STATE_ADDRESS = 0x14017B760L;

        private const long GLUT_SET_CURSOR_ADDRESS = 0x1408B68E6L;

        private const long TOUCH_REACTION_PLAY_TOUCH_EFF_ADDRESS = 0x1406A1F90L;

        private const long RESOLUTION_ADDRESS = 0x140EDA8BCL;

        private const float DEFAULT_RESOLUTION_WIDTH = 1280f;
        private const float DEFAULT_RESOLUTION_HEIGHT = 720f;

        private InputEmulator emulator = null;

        private ServerInterface server = null;

        private Queue<string> messageQueue = new Queue<string>();

        private Stopwatch performanceWatch = new Stopwatch();

        private int performanceCounter = 0;

        private float resolutionFactorX = 1f;
        private float resolutionFactorY = 1f;
        
        private bool checkSetCursor = true;
        private bool checkResolution = true;

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
                    new IntPtr(POLL_INPUT_FUNC_ADDRESS),
                    new VoidDelegate(PollInputOverride),
                    this),

                LocalHook.Create(
                    new IntPtr(TOUCH_REACTION_PLAY_TOUCH_EFF_ADDRESS),
                    new PlayAetTouchEffDelegate(PlayAetTouchEffOverride),
                    this),

                //LocalHook.Create(
                //    new IntPtr(LOG_FUNC_ADDRESS),
                //    new LogDelegate(LogOverride),
                //    this),

                //LocalHook.Create(
                //    new IntPtr(LOG_GAME_STATE_ADDRESS),
                //    new LogGameStateDelegate(LogGameStateOverride),
                //    this),
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

        #region Delegate Declarations
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void VoidDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void GlutSetCursorDelegate(GlutCursor cursor);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate void LogDelegate(long unk0, string formatString, string format, long unk1, long unk2, long unk3, long unk4);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate void LogGameStateDelegate(long formatString, long length, long format0, long format1);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate void PlayAetTouchEffDelegate(long touchReactionPtr, long position);
        #endregion

        #region Delegate Instances
        private static readonly GlutSetCursorDelegate GlutSetCursor = GetDelegateForFunctionPointer<GlutSetCursorDelegate>(GLUT_SET_CURSOR_ADDRESS);

        private static readonly LogGameStateDelegate LogGameState = GetDelegateForFunctionPointer<LogGameStateDelegate>(LOG_GAME_STATE_ADDRESS);

        private static readonly PlayAetTouchEffDelegate PlayAetTouchEff = GetDelegateForFunctionPointer<PlayAetTouchEffDelegate>(TOUCH_REACTION_PLAY_TOUCH_EFF_ADDRESS);
        #endregion

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

        private void LogOverride(long unk0, string formatString, string format, long unk1, long unk2, long unk3, long unk4)
        {
            if (unk3 == -2)
                formatString = Sprintf(formatString, format);

            server.ReportString(formatString);
        }

        private void LogGameStateOverride(long formatString, long length, long format0, long format1)
        {
            //server.ReportMessage($"{unk0}, {unk1}, {unk2}");
            //server.ReportString(formatString);
            server.ReportMessage($"LogGameStateOverride(): test");

            LogGameState(formatString, length, format0, format1);
        }

        private unsafe void PlayAetTouchEffOverride(long touchReactionPtr, long position)
        {
            if (checkResolution)
            {
                checkResolution = false;

                int width = emulator.MemoryManipulator.ReadInt32(RESOLUTION_ADDRESS);
                int height = emulator.MemoryManipulator.ReadInt32(RESOLUTION_ADDRESS + sizeof(int));

                resolutionFactorX = DEFAULT_RESOLUTION_WIDTH / width;
                resolutionFactorY = DEFAULT_RESOLUTION_HEIGHT / height;
            }

            float x = *((float*)&position + 0);
            float y = *((float*)&position + 1);

            x *= resolutionFactorX;
            y *= resolutionFactorY;

            *((int*)&position + 0) = *(int*)&x;
            *((int*)&position + 1) = *(int*)&y;

            PlayAetTouchEff(touchReactionPtr, position);
        }
        #endregion
    }
}