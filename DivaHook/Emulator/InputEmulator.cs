using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Camera;
using DivaHook.Emulator.Components;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator
{
    public class InputEmulator
    {
        public static readonly KeyConfig KeyConfig = new KeyConfig();

        public static readonly TimeSpan InputUpdateInterval = TimeSpan.FromMilliseconds(1000.0 / 144.0);

        public static readonly TimeSpan ProcessActiveCheckInterval = TimeSpan.FromMilliseconds(350.0);

        private const long NETWORK_COUNTER_CHECK_ADDRESS = 0x000000014CC95140L;

        private const long SYS_TIMER_OBJECT_ADDRESS = 0x000000014CC11960L;

        private const int SYS_TIMER_TIME_FACTOR = 60;

        public bool LogToConsole { get; set; } = true;

        public bool UseCameraControls { get; set; } = false;

        public bool IsDivaActive { get; internal set; } = false;

        public MemoryManipulator MemoryManipulator { get; private set; }

        public CameraController CameraController { get; private set; }

        public JvsEmulator JVSEmulator { get; private set; }

        public Stopwatch CheckProcessActiveStopwatch { get; internal set; } = new Stopwatch();

        public Stopwatch DeltaTimeStopwatch { get; private set; } = new Stopwatch();

        public TimeSpan DeltaTime { get; private set; }

        private IEmulatorComponent[] emulatorComponents = null;

        private StringBuilder previousConsoleBuffer = new StringBuilder();
        private StringBuilder consoleBuffer = new StringBuilder();

        private bool appendExit = false;
        private bool checkDs4 = true;

        public InputEmulator()
        {
            MemoryManipulator = new MemoryManipulator();
            MemoryManipulator.TryAttachToProcess("diva");

            emulatorComponents = new IEmulatorComponent[]
            {
                JVSEmulator = new JvsEmulator(MemoryManipulator, KeyConfig),
                new CoinEmulator(MemoryManipulator, KeyConfig),
                new TouchSliderEmulator(MemoryManipulator, KeyConfig),
                new TouchPanelEmulator(MemoryManipulator, KeyConfig),
                new StageManager(MemoryManipulator, KeyConfig),
            };

            CameraController = new CameraController(MemoryManipulator, KeyConfig);

            InitializeDivaMemory();

            string title = "process not found";

            if (MemoryManipulator.AttachedProcess != null)
            {
                title = $"{MemoryManipulator.AttachedProcess.ProcessName} - {MemoryManipulator.AttachedProcess.Id}";
            }
            else
            {
                appendExit = true;
            }

            Console.Title = $"DIVA Input Emulator: {title}";
            Console.CursorVisible = false;
        }

        public void LoopUpdateEmulateInput()
        {
            CheckProcessActiveStopwatch.Start();

            while (!appendExit)
            {
                UpdateEmulatorInputTick(IsDivaActive);

                if (CheckProcessActiveStopwatch.Elapsed >= ProcessActiveCheckInterval)
                {
                    IsDivaActive = MemoryManipulator.IsAttachedProcessActive();
                    CheckProcessActiveStopwatch.Restart();
                }

                Thread.Sleep(InputUpdateInterval);
            }
        }

        public void UpdateEmulatorInputTick(bool isProcessActive)
        {
            if (checkDs4)
            {
                Ds4Device.Instance = new Ds4Device();
                Ds4Device.Instance.StartDeviceUpdate();

                checkDs4 = false;
            }

            if (LogToConsole)
            {
                if (ShouldRedrawScreen())
                {
                    Console.Clear();
                    Console.Out.WriteLineAsync(consoleBuffer.ToString());
                }

                RefreshTextBuffer();
            }

            if (isProcessActive)
            {
                InputHelper.UpdateInputState();

                if (Ds4Device.Instance.IsConnected)
                    Ds4Device.Instance.PollInput();

                if (KeyConfig.ToggleCameraControl.IsAnyTapped())
                {
                    ToggleCameraControls();
                }

                if (UseCameraControls)
                {
                    CameraController.UpdateInputTick(DeltaTime);
                }
                else
                {
                    foreach (var emulator in emulatorComponents)
                        emulator.UpdateEmulatorTick(DeltaTime);
                }

                if (LogToConsole)
                {
                    consoleBuffer.Append($"Input Structure Addresses:\n" +
                        $"  0x{JVSEmulator.GetKeyDownStateAddress():X}: 0x{(int)JVSEmulator.JvsDownState:X8}\n" +
                        $"  0x{JVSEmulator.GetKeyTappedStateAddress():X}: 0x{(int)JVSEmulator.JvsTappedState:X8}\n");

                    consoleBuffer.AppendLine();
                    consoleBuffer.AppendLine($"down:    {JVSEmulator.JvsDownState}");
                    consoleBuffer.AppendLine($"tapped:  {JVSEmulator.JvsTappedState}");
                }
            }
            else
            {
                if (LogToConsole)
                {
                    consoleBuffer.AppendLine($"<diva inactive>");
                }
            }

            DeltaTime = DeltaTimeStopwatch.Elapsed;
            DeltaTimeStopwatch.Restart();
        }

        private void InitializeDivaMemory()
        {
            // FREE PLAY
            //MemoryManipulator.WriteByte(0x14066E878, 1); 

            // skip DHCP timer
            if (MemoryManipulator.ReadInt32(NETWORK_COUNTER_CHECK_ADDRESS) == 1)
                MemoryManipulator.WriteInt32(NETWORK_COUNTER_CHECK_ADDRESS, 2);

            foreach (var emulator in emulatorComponents)
                emulator.InitializeDivaMemory();

            // sys_timer
            {
                const int SYS_TIMER_TIME = 39 * SYS_TIMER_TIME_FACTOR;
                MemoryManipulator.WriteInt32(GetSysTimerTimeAddress(), SYS_TIMER_TIME);

                // 0x00000001405C5143:  mov qword ptr [rsi+0B38h], 3600
                MemoryManipulator.WriteInt32(0x1405C514AL, SYS_TIMER_TIME);

                // 0x00000001405BDFBF:  dec dword ptr [rbx+0B38h]
                MemoryManipulator.Write(0x1405BDFBFL, Assembly.GetNopInstructions(6));

                // 0x00000001405C517A:  mov [rsi+0B38h], ecx
                MemoryManipulator.Write(0x1405C517AL, Assembly.GetNopInstructions(6));
            }
        }

        private bool ShouldRedrawScreen()
        {
            if (consoleBuffer.Length != previousConsoleBuffer.Length)
                return true;

            bool redraw = false;

            for (int i = 0; i < consoleBuffer.Length; i++)
                redraw &= consoleBuffer[i] != previousConsoleBuffer[i];

            return redraw;
        }

        private void RefreshTextBuffer()
        {
            previousConsoleBuffer.Clear();
            previousConsoleBuffer = new StringBuilder(consoleBuffer.ToString());
            consoleBuffer.Clear();
        }

        private long GetSysTimerTimeAddress()
        {
            return SYS_TIMER_OBJECT_ADDRESS + 0xB38;
        }

        private void ToggleCameraControls()
        {
            if (UseCameraControls)
            {
                CameraController.DisableFreeCameraControls();
            }
            else
            {
                CameraController.EnableFreeCameraControls();
            }

            UseCameraControls = !UseCameraControls;
        }
    }
}