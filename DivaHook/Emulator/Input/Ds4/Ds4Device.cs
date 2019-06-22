using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows;

namespace DivaHook.Emulator.Input.Ds4
{
    public sealed class Ds4Device
    {
        private readonly Ds4Button[] definedButtons;

        public const int TOUCH_RANGE_X = 1920;
        public const int TOUCH_RANGE_Y = 920;

        public static Ds4Device Instance { get; internal set; }

        public DS4Device Device { get; private set; }

        public bool IsConnected { get; private set; }

        public DS4State CurrentState { get; private set; }

        public DS4State PreviousState { get; private set; }

        public Touch[] Touches { get; private set; }

        public bool IsTouched { get; private set; }

        internal Ds4Device()
        {
            CheckDeviceState();

            if (IsConnected)
            {
                CurrentState = new DS4State();
                PreviousState = new DS4State();
            }

            definedButtons = Enum.GetValues(typeof(Ds4Button)).Cast<Ds4Button>().ToArray();
        }

        public void StartDeviceUpdate()
        {
            if (!IsConnected)
                return;

            {
                Device.Report += OnReport;
                Device.Removal += OnRemoval;
                Device.Touchpad.TouchesMoved += OnTouchesMoved;
                Device.Touchpad.TouchesBegan += OnTouchesBegan;
                Device.Touchpad.TouchesEnded += OnTouchesEnded;
            }

            Device.Latency = 0;
            Device.StartUpdate();
        }

        public void PollInput()
        {
            if (!IsConnected)
                return;

            try
            {
                Device.PollDs4Input();
            }
            catch (Exception exception)
            {
                IsConnected = false;
                Device.StopUpdate();

                Console.WriteLine(exception);
            }
        }

        private void CheckDeviceState()
        {
            DS4Devices.findControllers();
            IEnumerable<DS4Device> devices = DS4Devices.getDS4Controllers();

            Device = devices.FirstOrDefault();

            IsConnected = Device != null;

            if (!IsConnected)
                return;
        }

        private void OnReport(object sender, EventArgs args)
        {
            Device.getCurrentState(CurrentState);
            Device.getPreviousState(PreviousState);

            CurrentState.CalculateStickAngles();
        }

        private void OnRemoval(object sender, EventArgs args)
        {
            IsConnected = false;
        }

        private void OnTouchesMoved(object sender, TouchpadEventArgs args)
        {
            Touches = args.touches;
        }

        private void OnTouchesBegan(object sender, TouchpadEventArgs e)
        {
            IsTouched = true;
        }

        private void OnTouchesEnded(object sender, TouchpadEventArgs e)
        {
            IsTouched = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDown(Ds4Button button)
        {
            return Ds4Helper.IsButtonDown(CurrentState, button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WasDown(Ds4Button button)
        {
            return Ds4Helper.IsButtonDown(PreviousState, button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUp(Ds4Button button)
        {
            return !IsDown(button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WasUp(Ds4Button button)
        {
            return !WasDown(button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTapped(Ds4Button button)
        {
            return IsDown(button) && WasUp(button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsReleased(Ds4Button button)
        {
            return WasDown(button) && IsUp(button);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyDown(params Ds4Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsDown(buttons[i]))
                    return true;

            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyTapped(params Ds4Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsTapped(buttons[i]))
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyReleased(Ds4Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsReleased(buttons[i]))
                    return true;
            }
            return false;
        }
    }
}