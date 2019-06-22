using System;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input.Ds4;
using System.Diagnostics;

namespace DivaHook.Emulator.Components
{
    public class TouchSliderEmulator : IEmulatorComponent
    {
        public static readonly TimeSpan TouchSliderTappedThreshold = TimeSpan.FromMilliseconds(MS_PER_FRAME * 4.0);

        private const long SLIDER_CTRL_TASK_OBJECT_ADDRESS = 0x000000014CC5DE40L;

        private const int SLIDER_SENSOR_ON_THRESHOLD_COUNT = 35;

        private const int SLIDER_SENSOR_PRESSURE_LEVEL = 180;

        private const int SLIDER_SENSOR_COUNT = 32;

        private const int SLIDER_SECTION_COUNT = 4;

        private const int SLIDER_HALF_SENSOR_COUNT = SLIDER_SENSOR_COUNT / 2;

        private const double HOLD_SLIDE_SPEED = 1.0;

        private const double TAPPED_SLIDE_SPEED = 0.5;

        private const double FRAME_RATE = 60.0;

        private const double MS_PER_FRAME = 1000.0 / FRAME_RATE;

        private const byte TRUE = 1, FALSE = 0;

        private const int SLIDER_OK = 3;

        private readonly double[] sliderIndexes = new double[SLIDER_SECTION_COUNT];

        private readonly Stopwatch[] sliderStopwatched = new Stopwatch[SLIDER_SECTION_COUNT];

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public TouchSliderEmulator(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;

            for (int i = 0; i < sliderStopwatched.Length; i++)
                sliderStopwatched[i] = new Stopwatch();
        }

        public void InitializeDivaMemory()
        {
            // only two will be used for the touch slider emulation
            for (int i = 0; i < SLIDER_SECTION_COUNT; i++)
                MemoryManipulator.WriteInt32(GetTouchSliderIsSectionConnectedAddress(i), -1);
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            if (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsTouched)
            {
                UpdateDs4TouchpadTick(deltaTime);
                return;
            }

            // enable touch slider connection
            MemoryManipulator.WriteInt32(GetSliderConnectionStateAddress(), SLIDER_OK);

            bool leftLeftDown = KeyConfig.LeftSideSlideLeftBinding.IsAnyDown();
            bool leftRightDown = KeyConfig.LeftSideSlideRightBinding.IsAnyDown();

            bool rightLeftDown = KeyConfig.RightSideSlideLeftBinding.IsAnyDown();
            bool rightRightDown = KeyConfig.RightSideSlideRightBinding.IsAnyDown();

            bool leftSideUsed = leftLeftDown || leftRightDown;
            bool rightSideUsed = rightLeftDown || rightRightDown;

            int heldDownCount = Convert.ToByte(leftLeftDown) + Convert.ToByte(leftRightDown) + Convert.ToByte(rightLeftDown) + Convert.ToByte(rightRightDown);

            for (int section = 0; section < SLIDER_SECTION_COUNT; section++)
                SetSection(section, Convert.ToByte(heldDownCount <= section));

            EmulateSliderInput(sectionIndex: 0, leftSideOfTouchPanel: leftSideUsed, slideLeft: true, binding: KeyConfig.LeftSideSlideLeftBinding);
            EmulateSliderInput(sectionIndex: 1, leftSideOfTouchPanel: !leftLeftDown && leftSideUsed, slideLeft: false, binding: KeyConfig.LeftSideSlideRightBinding);

            EmulateSliderInput(sectionIndex: 2, leftSideOfTouchPanel: !leftSideUsed, slideLeft: true, binding: KeyConfig.RightSideSlideLeftBinding);
            EmulateSliderInput(sectionIndex: 3, leftSideOfTouchPanel: !rightLeftDown && !leftSideUsed, slideLeft: false, binding: KeyConfig.RightSideSlideRightBinding);

            if (!leftSideUsed && !rightSideUsed)
            {
                ResetSensors(0, SLIDER_SENSOR_COUNT);

                for (int section = 0; section < SLIDER_SECTION_COUNT; section++)
                {
                    MemoryManipulator.WriteSingle(GetTouchSliderSectionTouchPositonAddress(section), 0f);
                    SetSection(section, FALSE);
                }
            }
            else if (heldDownCount == 1)
            {
                ResetSensors(SLIDER_HALF_SENSOR_COUNT, SLIDER_SENSOR_COUNT);
            }

            void EmulateSliderInput(int sectionIndex, bool leftSideOfTouchPanel, bool slideLeft, ControlBinding binding)
            {
                int sensorRangeStart = SLIDER_HALF_SENSOR_COUNT * Convert.ToInt32(!leftSideOfTouchPanel);
                int sensorRangeEnd = sensorRangeStart + SLIDER_HALF_SENSOR_COUNT;

                if (binding.IsAnyTapped())
                {
                    sliderIndexes[sectionIndex] = 0;
                    MemoryManipulator.WriteInt32(GetTouchSliderIsSectionConnectedAddress(Convert.ToInt32(!slideLeft)), TRUE);
                }

                if (binding.IsAnyTapped())
                {
                    sliderStopwatched[sectionIndex].Restart();
                }

                if (binding.IsAnyDown())
                {
                    TimeSpan heldDownDuration = sliderStopwatched[sectionIndex].Elapsed;

                    bool fastSlide = heldDownDuration < TouchSliderTappedThreshold;
                    double slideSpeed = fastSlide ? TAPPED_SLIDE_SPEED : HOLD_SLIDE_SPEED;

                    sliderIndexes[sectionIndex] += (deltaTime.TotalMilliseconds / MS_PER_FRAME / slideSpeed);

                    int index = (int)sliderIndexes[sectionIndex];

                    int durationIndex = index % (SLIDER_HALF_SENSOR_COUNT + 1);

                    int sensorIndex = !slideLeft ? durationIndex : SLIDER_HALF_SENSOR_COUNT - durationIndex;

                    ResetSensors(sensorRangeStart, sensorRangeEnd);

                    for (int i = -1; i < 3 - 1; i++)
                        SetSliderSensor(ClampSensorIndex(sensorIndex + i));
                }

                if (binding.IsAnyReleased())
                {
                    ResetSensors(sensorRangeStart, sensorRangeEnd);
                }

                float position = slideLeft ? -1.5f : +1.5f;
                MemoryManipulator.WriteSingle(GetTouchSliderSectionTouchPositonAddress(sectionIndex), binding.IsAnyDown() ? position : FALSE);

                int ClampSensorIndex(int index) => MathHelper.Clamp(sensorRangeStart + index, sensorRangeStart, sensorRangeEnd - 1);
            }
        }

        private void UpdateDs4TouchpadTick(TimeSpan deltaTime)
        {
            var ds4 = Ds4Device.Instance;

            if (ds4.Touches != null)
            {
                ResetSensors(0, SLIDER_SENSOR_COUNT);

                for (int section = 0; section < ds4.Touches.Length; section++)
                {
                    MemoryManipulator.WriteByte(GetTouchSliderIsSectionTouchedAddress(section), Convert.ToByte(ds4.IsTouched));

                    if (ds4.IsTouched)
                    {
                        int touchXPos = ds4.Touches[section].hwX;

                        SetSliderSensor(touchXPos * SLIDER_SENSOR_COUNT / Ds4Device.TOUCH_RANGE_X);

                        MemoryManipulator.WriteSingle(
                            GetTouchSliderSectionTouchPositonAddress(section), 
                            MathHelper.ConvertRange(0, Ds4Device.TOUCH_RANGE_X, -2.5f, +2.5f, touchXPos));
                    }
                }
            }
        }

        private void SetSection(int sectionIndex, byte value)
        {
            MemoryManipulator.WriteByte(GetTouchSliderSectionTouchPositonAddress(sectionIndex), value);
            MemoryManipulator.WriteByte(GetTouchSliderIsSectionTouchedAddress(sectionIndex), value);
        }

        private void SetSliderSensor(int sensorIndex, int value = SLIDER_SENSOR_PRESSURE_LEVEL)
        {
            MemoryManipulator.WriteInt32(GetSliderIsOnAddress(sensorIndex), value >= SLIDER_SENSOR_ON_THRESHOLD_COUNT ? TRUE : FALSE);
            MemoryManipulator.WriteInt32(GetSliderPressureValueAddress(sensorIndex), value);
        }

        private void ResetSensors(int startRange, int endRange)
        {
            for (int sensorIndex = startRange; sensorIndex < endRange; sensorIndex++)
                SetSliderSensor(sensorIndex, FALSE);
        }

        private long GetTouchSliderIsSectionConnectedAddress(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= SLIDER_SECTION_COUNT)
                return 0L;
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x4 * sectionIndex + 0x14CL;
        }

        private long GetTouchSliderIsSectionTouchedAddress(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= SLIDER_SECTION_COUNT)
                return 0L;
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x1 * sectionIndex + 0x160L;
        }

        private long GetTouchSliderSectionTouchPositonAddress(int sectionIndex)
        {
            // -1.5f <-> +1.5f
            if (sectionIndex < 0 || sectionIndex >= SLIDER_SECTION_COUNT)
                return 0L;
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x4 * sectionIndex + 0x13CL;
        }

        private long GetSliderIsOnAddress(int sliderIndex)
        {
            if (sliderIndex < 0 || sliderIndex >= SLIDER_SENSOR_COUNT)
                return 0L;
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x30 * sliderIndex + 0xD42L;
        }

        private long GetSliderPressureValueAddress(int sliderIndex)
        {
            if (sliderIndex < 0 || sliderIndex >= SLIDER_SENSOR_COUNT)
                return 0L;
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x4 * sliderIndex + 0x94L;
        }

        private long GetSliderConnectionStateAddress()
        {
            return SLIDER_CTRL_TASK_OBJECT_ADDRESS + 0x70L;
        }
    }
}