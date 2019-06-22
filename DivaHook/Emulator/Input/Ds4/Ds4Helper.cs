using DS4Windows;

namespace DivaHook.Emulator.Input.Ds4
{
    public static class Ds4Helper
    {
        public const byte TriggerThreshold = 30;

        public const byte StickThreshold = 45;

        public const byte StickUpperThreshold = (byte.MaxValue / 2) + StickThreshold;
        public const byte StickLowerThreshold = (byte.MaxValue / 2) - StickThreshold;

        const double LeftAngleUpThreshold = 25.0;
        const double LeftAngleDownThreshold = 180.0 - LeftAngleUpThreshold;

        const double RightAngleUpThreshold = 360.0 - LeftAngleUpThreshold;
        const double RightAngleDownThreshold = 180.0 + LeftAngleUpThreshold;

        public static bool IsButtonDown(DS4State state, Ds4Button button)
        {
            switch (button)
            {
                case Ds4Button.None: return false;
                case Ds4Button.Maru: return state.Circle;
                case Ds4Button.Batsu: return state.Cross;
                case Ds4Button.Shikaku: return state.Square;
                case Ds4Button.Sankaku: return state.Triangle;
                case Ds4Button.DpadRight: return state.DpadRight;
                case Ds4Button.DpadDown: return state.DpadDown;
                case Ds4Button.DpadLeft: return state.DpadLeft;
                case Ds4Button.DpadUp: return state.DpadUp;
                case Ds4Button.Options: return state.Options;
                case Ds4Button.Share: return state.Share;
                case Ds4Button.PS: return state.PS;

                case Ds4Button.R1: return state.R1;
                case Ds4Button.L1: return state.L1;

                case Ds4Button.R2: return state.R2 >= TriggerThreshold;
                case Ds4Button.L2: return state.L2 >= TriggerThreshold;

                case Ds4Button.R3: return state.R3;
                case Ds4Button.L3: return state.L3;

                case Ds4Button.LeftStickLeft:
                    return state.LX <= StickLowerThreshold;
                case Ds4Button.LeftStickRight:
                    return state.LX >= StickUpperThreshold;

                case Ds4Button.RightStickLeft:
                    return state.RX <= StickLowerThreshold;
                case Ds4Button.RightStickRight:
                    return state.RX >= StickUpperThreshold;

                case Ds4Button.TouchButton: return state.TouchButton;
                case Ds4Button.TouchLeft: return state.TouchLeft;
                case Ds4Button.TouchRight: return state.TouchRight;
                default: return false;
            }
        }
    }
}