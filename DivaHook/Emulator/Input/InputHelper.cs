using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DivaHook.Emulator.Input
{
    public class InputHelper
    {
        private readonly Dictionary<Keys, Stopwatch> keyHeldDownDurations;

        private static InputHelper _instance;

        public static InputHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new InputHelper();

                return _instance;
            }
            private set => _instance = value;
        }

        private InputHelper()
        {
            Array keyValues = Enum.GetValues(typeof(Keys));

            Keys[] allKeys = new Keys[keyValues.Length];

            keyValues.CopyTo(allKeys, 0);

            keyHeldDownDurations = allKeys.ToDictionary(t => t, t => new Stopwatch());

            return;
        }

        public KeyboardState CurrentKeyboardState { get; private set; }

        public KeyboardState PreviousKeyboardState { get; private set; }

        public MouseState CurrentMouseState { get; private set; }

        public MouseState PreviousMouseState { get; private set; }

        public static void UpdateInputState()
        {
            UpdateMouseState();
            UpdateKeyboardState();
        }

        private static void UpdateMouseState()
        {
            Instance.PreviousMouseState = Instance.CurrentMouseState;
            Instance.CurrentMouseState = Mouse.GetMouseState();
        }

        private static void UpdateKeyboardState()
        {
            Instance.PreviousKeyboardState = Instance.CurrentKeyboardState;
            Instance.CurrentKeyboardState = Keyboard.GetState();

            Keys[] pressedDown = GetPressedKeys();
            Keys[] lastPressedDown = GetLastPressedKeys();

            for (int i = 0; i < pressedDown.Length; i++)
            {
                if (IsTapped(pressedDown[i]))
                    Instance.keyHeldDownDurations[pressedDown[i]].Start();
            }

            for (int i = 0; i < lastPressedDown.Length; i++)
            {
                if (IsReleased(lastPressedDown[i]))
                    Instance.keyHeldDownDurations[lastPressedDown[i]].Reset();
            }
        }

        public static TimeSpan GetHeldDownDuration(Keys key)
        {
            return Instance.keyHeldDownDurations[key].Elapsed;
        }

        public static TimeSpan GetHeldDownDuration(Keys[] keys)
        {
            if (keys.Length < 1)
            {
                return TimeSpan.Zero;
            }
            else if (keys.Length == 1)
            {
                return Instance.keyHeldDownDurations[keys[0]].Elapsed;
            }
            else
            {
                TimeSpan[] durations = new TimeSpan[keys.Length];

                for (int i = 0; i < keys.Length; i++)
                    durations[i] = GetHeldDownDuration(keys[i]);

                return durations.Max();
            }
        }

        public bool HasMouseMoved() => CurrentMouseState.Position != PreviousMouseState.Position;

        public static Keys[] GetPressedKeys()
        {
            return Instance.CurrentKeyboardState.GetPressedKeys();
        }

        public static Keys[] GetLastPressedKeys()
        {
            return Instance.PreviousKeyboardState.GetPressedKeys();
        }

        public static bool IsDown(Keys key)
        {
            return Instance.CurrentKeyboardState.IsKeyDown(key);
        }

        public static bool IsUp(Keys key)
        {
            return Instance.CurrentKeyboardState.IsKeyUp(key);
        }

        public static bool IsTapped(Keys key)
        {
            return Instance.CurrentKeyboardState.IsKeyDown(key) && Instance.PreviousKeyboardState.IsKeyUp(key);
        }

        public static bool IsReleased(Keys key)
        {
            return Instance.CurrentKeyboardState.IsKeyUp(key) && Instance.PreviousKeyboardState.IsKeyDown(key);
        }

        public static bool AreAllUp(Keys[] keyBinding)
        {
            return !IsAnyDown(keyBinding);
        }

        public static bool IsAnyDown(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsDown(keys[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AreAllDown(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (!IsDown(keys[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsAnyTapped(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsTapped(keys[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool WasAnyTapped(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsReleased(keys[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsAnyReleased(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsReleased(keys[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AreAllReleased(params Keys[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (!IsReleased(keys[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
