using System.Linq;
using System.Text;
using System.Collections.Generic;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Components;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator.Config
{
    public sealed class KeyConfig : ConfigFile
    {
        public override string ConfigFileName => "KeyConfig.ini";

        public ControlBinding SkipNetworkBinding = new ControlBinding(Keys.F4);

        public ControlBinding InsertCoinBinding = new ControlBinding(Keys.Back, Ds4Button.Share);

        public ControlBinding TestButtonBinding = new ControlBinding(Keys.F1);
        public ControlBinding ServiceButtonBinding = new ControlBinding(Keys.F2);
        public ControlBinding StartButtonBinding = new ControlBinding(Keys.Enter, Ds4Button.Options );

        public ControlBinding TriangleButtonBinding = new ControlBinding(Keys.W, Keys.I, Ds4Button.Sankaku, Ds4Button.DpadUp, Ds4Button.L2, Ds4Button.R2);
        public ControlBinding SquareButtonBinding = new ControlBinding(Keys.A, Keys.J, Ds4Button.Shikaku, Ds4Button.DpadLeft, Ds4Button.L2, Ds4Button.R2);
        public ControlBinding CrossButtonBinding = new ControlBinding(Keys.S, Keys.K, Ds4Button.Batsu, Ds4Button.DpadDown, Ds4Button.L2, Ds4Button.R2);
        public ControlBinding CircleButtonBinding = new ControlBinding(Keys.D, Keys.L, Ds4Button.Maru, Ds4Button.DpadRight, Ds4Button.L2, Ds4Button.R2);

        public ControlBinding LeftSideSlideLeftBinding = new ControlBinding(Keys.Q, Ds4Button.LeftStickLeft, Ds4Button.L1);
        public ControlBinding LeftSideSlideRightBinding = new ControlBinding(Keys.E, Ds4Button.LeftStickRight, Ds4Button.R1);

        public ControlBinding RightSideSlideLeftBinding = new ControlBinding(Keys.U, Ds4Button.RightStickLeft);
        public ControlBinding RightSideSlideRightBinding = new ControlBinding(Keys.O, Ds4Button.RightStickRight);

        public ControlBinding ToggleCameraControl = new ControlBinding(Keys.F5);

        public ControlBinding FastCameraSpeedBinding = new ControlBinding(Keys.LeftShift);

        public ControlBinding MoveCameraForwardBinding = new ControlBinding(Keys.W);
        public ControlBinding MoveCameraBackwardBinding = new ControlBinding(Keys.S);
        public ControlBinding MoveCameraLeftBinding = new ControlBinding(Keys.A);
        public ControlBinding MoveCameraRightBinding = new ControlBinding(Keys.D);

        public ControlBinding MoveCameraUpBinding = new ControlBinding(Keys.Space);
        public ControlBinding MoveCameraDownBinding = new ControlBinding(Keys.LeftControl);

        public ControlBinding IncreaseCameraFovBinding = new ControlBinding(Keys.R);
        public ControlBinding DecreaseCameraFovBinding = new ControlBinding(Keys.F);
        public ControlBinding ResetCameraFovBinding = new ControlBinding(Keys.T);

        private readonly Dictionary<string, ControlBindingAccessor> accessorDictionary = null;

        public KeyConfig()
        {
            accessorDictionary = GetAccessorDictionary();
        }

        protected override void ParseLine(string line)
        {
            string[] keyValuePair = line.Split(KeyValueSeperator, 2);

            if (keyValuePair.Length < 2)
                return;

            string keyString = keyValuePair[0].Trim();
            string[] valueStrings = keyValuePair[1].Split(ValueSeperator).Select(s => s = s.Trim()).ToArray();

            if (accessorDictionary.TryGetValue(keyString, out var accessor))
            {
                if (ControlBinding.TryParse(valueStrings, out ControlBinding binding))
                    accessor.Setter?.Invoke(binding);
            }
        }

        protected override void FormatFile(StringBuilder builder)
        {
            string valueSeperator = $"{ValueSeperator} ";

            foreach (var entry in accessorDictionary)
            {
                var binding = entry.Value.Getter();

                object[] buffer = new object[binding.Keys.Length + binding.Buttons.Length];

                string value = string.Join(valueSeperator, binding.Keys.Select(k => k.ToString()).Concat(binding.Buttons.Select(b => b.ToString())));

                builder.AppendLine($"{entry.Key} {KeyValueSeperator[0]} {value}");
            }
        }

        private Dictionary<string, ControlBindingAccessor> GetAccessorDictionary()
        {
            return new Dictionary<string, ControlBindingAccessor>()
            {
                {
                    "SKIP_NETWORK_CHECKS", new ControlBindingAccessor(() => SkipNetworkBinding, (v) => SkipNetworkBinding = v)
                },
                {
                    "INSERT_COIN", new ControlBindingAccessor(() => InsertCoinBinding, (v) => InsertCoinBinding = v)
                },

                {
                    JvsButtons.JVS_TEST.ToString(), new ControlBindingAccessor(() => TestButtonBinding, (v) => TestButtonBinding = v)
                },
                {
                    JvsButtons.JVS_SERVICE.ToString(), new ControlBindingAccessor(() => ServiceButtonBinding, (v) => ServiceButtonBinding = v)
                },
                {
                    JvsButtons.JVS_START.ToString(), new ControlBindingAccessor(() => StartButtonBinding, (v) => StartButtonBinding = v)
                },

                {
                    JvsButtons.JVS_TRIANGLE.ToString(), new ControlBindingAccessor(() => TriangleButtonBinding, (v) => TriangleButtonBinding = v)
                },
                {
                    JvsButtons.JVS_SQUARE.ToString(), new ControlBindingAccessor(() => SquareButtonBinding, (v) => SquareButtonBinding = v)
                },
                {
                    JvsButtons.JVS_CROSS.ToString(), new ControlBindingAccessor(() => CrossButtonBinding, (v) => CrossButtonBinding = v)
                },
                {
                    JvsButtons.JVS_CIRCLE.ToString(), new ControlBindingAccessor(() => CircleButtonBinding, (v) => CircleButtonBinding = v)
                },

                {
                    "LEFT_SIDE_SLIDE_LEFT", new ControlBindingAccessor(() => LeftSideSlideLeftBinding, (v) => LeftSideSlideLeftBinding = v)
                },
                {
                    "LEFT_SIDE_SLIDE_RIGHT", new ControlBindingAccessor(() => LeftSideSlideRightBinding, (v) => LeftSideSlideRightBinding = v)
                },
                {
                    "RIGHT_SIDE_SLIDE_LEFT", new ControlBindingAccessor(() => RightSideSlideLeftBinding, (v) => RightSideSlideLeftBinding = v)
                },
                {
                    "RIGHT_SIDE_SLIDE_RIGHT", new ControlBindingAccessor(() => RightSideSlideRightBinding, (v) => RightSideSlideRightBinding = v)
                },

                {
                    "TOGGLE_CAMERA_CONTROL", new ControlBindingAccessor(() => ToggleCameraControl, (v) => ToggleCameraControl = v)
                },

                {
                    "FAST_CAMERA_SPEED", new ControlBindingAccessor(() => FastCameraSpeedBinding, (v) => FastCameraSpeedBinding = v)
                },
                {
                    "MOVE_CAMERA_FORWARD", new ControlBindingAccessor(() => MoveCameraForwardBinding, (v) => MoveCameraForwardBinding = v)
                },
                {
                    "MOVE_CAMERA_BACKWARD", new ControlBindingAccessor(() => MoveCameraBackwardBinding, (v) => MoveCameraBackwardBinding = v)
                },
                {
                    "MOVE_CAMERA_LEFT", new ControlBindingAccessor(() => MoveCameraLeftBinding, (v) => MoveCameraLeftBinding = v)
                },
                {
                    "MOVE_CAMERA_RIGHT", new ControlBindingAccessor(() => MoveCameraRightBinding, (v) => MoveCameraRightBinding = v)
                },
                {
                    "MOVE_CAMERA_UP", new ControlBindingAccessor(() => MoveCameraUpBinding, (v) => MoveCameraUpBinding = v)
                },
                {
                    "MOVE_CAMERA_DOWN", new ControlBindingAccessor(() => MoveCameraDownBinding, (v) => MoveCameraDownBinding = v)
                },
                {
                    "INCREASE_CAMERA_FOV", new ControlBindingAccessor(() => IncreaseCameraFovBinding, (v) => IncreaseCameraFovBinding = v)
                },
                {
                    "DECREASE_CAMERA_FOV", new ControlBindingAccessor(() => DecreaseCameraFovBinding, (v) => DecreaseCameraFovBinding = v)
                },
                {
                    "RESET_CAMERA_FOV", new ControlBindingAccessor(() => ResetCameraFovBinding, (v) => ResetCameraFovBinding = v)
                },
            };
        }
    }
}
