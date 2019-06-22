using System;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator.Components
{
    public class JvsEmulator : IEmulatorComponent
    {
        private const long INPUT_STATE_STRUCT_PTR_ADDRESS = 0x0000000140EDA330L;

        private const long NETWORK_STATE_ADDRESS = 0x000000014CC95168L;

        public JvsButtons JvsTappedState { get; private set; } = JvsButtons.JVS_NONE;
        public JvsButtons JvsDownState { get; private set; } = JvsButtons.JVS_NONE;

        public JvsButtons PreviousJvsTappedState { get; private set; } = JvsButtons.JVS_NONE;
        public JvsButtons PreviousJvsDownState { get; private set; } = JvsButtons.JVS_NONE;

        public long InputStateStructurePointer { get; private set; }

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public JvsEmulator(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
            InputStateStructurePointer = MemoryManipulator.ReadInt64(INPUT_STATE_STRUCT_PTR_ADDRESS);
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            PreviousJvsTappedState = JvsTappedState;
            PreviousJvsDownState = JvsDownState;

            JvsDownState = GetKeyboardJvsState(ControlBinding.IsAnyDown);
            JvsTappedState = GetKeyboardJvsState(ControlBinding.IsAnyTapped);

            // repress held down buttons MikuComfy
            JvsDownState ^= JvsTappedState;

            bool performMemoryWrite =
                !(JvsDownState == JvsButtons.JVS_NONE && JvsTappedState == JvsButtons.JVS_NONE &&
                PreviousJvsDownState == JvsButtons.JVS_NONE && PreviousJvsTappedState == JvsButtons.JVS_NONE);

            if (performMemoryWrite)
            {
                MemoryManipulator.WriteInt32(GetKeyDownStateAddress(), (int)JvsDownState);
                MemoryManipulator.WriteInt32(GetKeyTappedStateAddress(), (int)JvsTappedState);
            }

            if (KeyConfig.SkipNetworkBinding.IsAnyTapped())
            {
                MemoryManipulator.WriteInt64(NETWORK_STATE_ADDRESS, -2);
            }
        }

        public long GetKeyTappedStateAddress()
        {
            return InputStateStructurePointer;
        }

        public long GetKeyDownStateAddress()
        {
            return InputStateStructurePointer + 0x20;
        }

        private JvsButtons GetKeyboardJvsState(Func<ControlBinding, bool> keyCheckFunc)
        {
            JvsButtons inputState = JvsButtons.JVS_NONE;

            if (keyCheckFunc(KeyConfig.TestButtonBinding))
                inputState |= JvsButtons.JVS_TEST;
            if (keyCheckFunc(KeyConfig.ServiceButtonBinding))
                inputState |= JvsButtons.JVS_SERVICE;
            if (keyCheckFunc(KeyConfig.StartButtonBinding))
                inputState |= JvsButtons.JVS_START;

            if (keyCheckFunc(KeyConfig.TriangleButtonBinding))
                inputState |= JvsButtons.JVS_TRIANGLE;
            if (keyCheckFunc(KeyConfig.SquareButtonBinding))
                inputState |= JvsButtons.JVS_SQUARE;
            if (keyCheckFunc(KeyConfig.CrossButtonBinding))
                inputState |= JvsButtons.JVS_CROSS;
            if (keyCheckFunc(KeyConfig.CircleButtonBinding))
                inputState |= JvsButtons.JVS_CIRCLE;

            return inputState;
        }
    }
}
