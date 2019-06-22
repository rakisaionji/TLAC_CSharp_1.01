using System;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator.Components
{
    public class TouchPanelEmulator : IEmulatorComponent
    {
        private const int TOUCH_PANEL_NG = 4;

        private const long TOUCH_PANEL_TASK_OBJECT_ADDRESS = 0x000000014CC9EC30L;

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        private bool checkTouchPanelState = true;

        public TouchPanelEmulator(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
            MemoryManipulator.WriteInt32(GetConnectionStateAddress(), 1);

            //MemoryManipulator.Write(
            //    TOUCH_PANEL_STATE_GETTER_ADDRESS, 
            //    new byte[] 
            //    {
            //        // mov eax, 1
            //        0xB8, 0x01, 0x00, 0x00, 0x00,
            //        // retn
            //        0xC3,
            //    });
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            // to not enable during SYSTEM STARTUP
            if (checkTouchPanelState && MemoryManipulator.ReadInt32(GetConnectionStateAddress()) == TOUCH_PANEL_NG)
            {
                MemoryManipulator.WriteInt32(GetConnectionStateAddress(), 1);
                checkTouchPanelState = false;
            }

            if (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsTouched)
            {
                MemoryManipulator.WriteInt32(GetAdvTouchIsTappedAddress(), 1);
            }

            bool tapped = InputHelper.IsTapped(Keys.MouseLeft);
            bool released = InputHelper.IsReleased(Keys.MouseLeft);

            int contactType = tapped ? 2 : released ? 1 : 0;
            MemoryManipulator.WriteInt32(GetTouchPanelContactTypeAddress(), contactType);

            if (InputHelper.Instance.HasMouseMoved())
            {
                var bounds = MemoryManipulator.GetMainWindowBounds();
                var mousePos = InputHelper.Instance.CurrentMouseState.Position - bounds.Position;

                MemoryManipulator.WriteSingle(GetTouchPanelXPositionAddress(), mousePos.X);
                MemoryManipulator.WriteSingle(GetTouchPanelYPositionAddress(), mousePos.Y);
            }

            // not sure what this is but it's zero checked before jumping to the TouchReaction function
            MemoryManipulator.WriteSingle(TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x9CL, 1); 
        }

        private long GetConnectionStateAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x78L;
        }

        private long GetTouchPanelContactTypeAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0xA0L;
        }

        private long GetTouchPanelXPositionAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x94L;
        }

        private long GetTouchPanelYPositionAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x98L;
        }

        private long GetAdvTouchIsTappedAddress()
        {
            return 0x140EC52D0L + 0x70L;
        }
    }
}
