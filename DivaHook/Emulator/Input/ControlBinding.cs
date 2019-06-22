using DivaHook.Emulator.Input.Ds4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DivaHook.Emulator.Input
{
    public class ControlBinding
    {
        public Keys[] Keys;

        public Ds4Button[] Buttons;

        public ControlBinding(Keys[] keys, Ds4Button[] buttons)
        {
            Keys = keys;
            Buttons = buttons;
        }

        public ControlBinding(params object[] values)
        {
            List<Keys> keys = new List<Keys>(values.Length);
            List<Ds4Button> buttons = new List<Ds4Button>(values.Length);

            foreach (var value in values)
            {
                if (value is Keys key)
                    keys.Add(key);
                else if (value is Ds4Button button)
                    buttons.Add(button);
            }

            Keys = keys.ToArray();
            Buttons = buttons.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyDown()
        {
            return InputHelper.IsAnyDown(Keys) || (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsAnyDown(Buttons));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyTapped()
        {
            return InputHelper.IsAnyTapped(Keys) || (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsAnyTapped(Buttons));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyReleased()
        {
            return InputHelper.IsAnyReleased(Keys) || (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsAnyReleased(Buttons));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyDown(ControlBinding binding) => binding.IsAnyDown();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyTapped(ControlBinding binding) => binding.IsAnyTapped();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyReleased(ControlBinding binding) => binding.IsAnyReleased();

        internal static bool TryParse(string[] values, out ControlBinding binding)
        {
            List<Keys> keys = new List<Keys>(values.Length);
            List<Ds4Button> buttons = new List<Ds4Button>(values.Length);

            foreach (var keyString in values)
            {
                if (Enum.TryParse(keyString, out Keys key))
                    keys.Add(key);
                else if (Enum.TryParse(keyString, out Ds4Button button))
                    buttons.Add(button);
            }

            binding = new ControlBinding(keys.ToArray(), buttons.ToArray());

            return true;
        }
    }
}