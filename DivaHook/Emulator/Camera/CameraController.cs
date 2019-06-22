using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Config;

namespace DivaHook.Emulator.Camera
{
    public class CameraController
    {
        private const long CAMERA_OBJECT_ADDRESS = 0x0000000140FBC2C0L;

        private const long CAMERA_COORDINATES_UPDATE_FUNC_ADDRESS = 0x00000001401F9460L;

        private const long CAMERA_FOCUS_COORDINATES_UPDATE_FUNC_ADDRESS = 0x00000001401F93F0L;

        private const long CAMERA_SLANT_UPDATE_FUNC_ADDRESS = 0x00000001401F9480L;

        private const long CAMERA_FOV_UPDATE_FUNC_ADDRESS = 0x00000001401F9430L;

        private const float DEFAULT_FOV = 90f;

        private const float MIN_FOV = 1f;
        private const float MAX_FOV = 170f;

        public MemoryManipulator MemoryManipulator { get; private set; }

        public KeyConfig KeyConfig { get; private set; }

        public CameraData CameraData;

        public float CameraVerticalRotation = 0f;
        public float CameraHorizontalRotation = 0f;

        public float CameraSlant = 0f;

        public float CameraSpeed = .0035f;

        public float FastCameraSpeed = .05f;

        private byte[] coordinatesFuncBytes;
        private byte[] focusCoordinatesFuncBytes;
        private byte[] slantFuncBytes;
        private byte[] fovFuncBytes;

        public CameraController(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
            return;
        }

        public void ReadDivaCameraData()
        {
            byte[] buffer = MemoryManipulator.Read(CAMERA_OBJECT_ADDRESS, CameraData.BYTE_SIZE);
            CameraData = CameraData.FromBytes(buffer);
        }

        public void WriteDivaCameraData()
        {
            MemoryManipulator.Write(CAMERA_OBJECT_ADDRESS, CameraData.GetBytes());
        }

        public void EnableFreeCameraControls()
        {
            Injection.InjectionEntryPoint.HideCursor();

            coordinatesFuncBytes = MemoryManipulator.Read(CAMERA_COORDINATES_UPDATE_FUNC_ADDRESS, 2);
            MemoryManipulator.Write(CAMERA_COORDINATES_UPDATE_FUNC_ADDRESS, Assembly.GetPaddedReturnInstructions(1));

            focusCoordinatesFuncBytes = MemoryManipulator.Read(CAMERA_FOCUS_COORDINATES_UPDATE_FUNC_ADDRESS, 2);
            MemoryManipulator.Write(CAMERA_FOCUS_COORDINATES_UPDATE_FUNC_ADDRESS, Assembly.GetPaddedReturnInstructions(1));

            slantFuncBytes = MemoryManipulator.Read(CAMERA_SLANT_UPDATE_FUNC_ADDRESS, 8);
            MemoryManipulator.Write(CAMERA_SLANT_UPDATE_FUNC_ADDRESS, Assembly.GetPaddedReturnInstructions(7));

            fovFuncBytes = MemoryManipulator.Read(CAMERA_FOV_UPDATE_FUNC_ADDRESS, 7);
            MemoryManipulator.Write(CAMERA_FOV_UPDATE_FUNC_ADDRESS, Assembly.GetPaddedReturnInstructions(6));

            ReadDivaCameraData();

            CameraVerticalRotation = MathHelper.AngleFromPoints(
                CameraData.X, CameraData.Z,
                CameraData.FocusX, CameraData.FocusZ);

            CameraHorizontalRotation = 0f;
            CameraSlant = 0f;
        }

        public void DisableFreeCameraControls()
        {
            Injection.InjectionEntryPoint.ShowCursor();

            MemoryManipulator.Write(CAMERA_COORDINATES_UPDATE_FUNC_ADDRESS, coordinatesFuncBytes);
            MemoryManipulator.Write(CAMERA_FOCUS_COORDINATES_UPDATE_FUNC_ADDRESS, focusCoordinatesFuncBytes);
            MemoryManipulator.Write(CAMERA_SLANT_UPDATE_FUNC_ADDRESS, slantFuncBytes);
            MemoryManipulator.Write(CAMERA_FOV_UPDATE_FUNC_ADDRESS, fovFuncBytes);
        }

        public void UpdateInputTick(TimeSpan deltaTime)
        {
            float elapsedMs = (float)deltaTime.TotalMilliseconds;

            bool fastCamera = InputEmulator.KeyConfig.FastCameraSpeedBinding.IsAnyDown();

            float cameraDistance = elapsedMs * (fastCamera ? FastCameraSpeed : CameraSpeed);

            ReadDivaCameraData();

            {
                var bounds = MemoryManipulator.GetMainWindowBounds();
                var center = bounds.Center;

                Vector2 mouseMovement = InputHelper.Instance.CurrentMouseState.Position - center.ToVector2();

                CameraVerticalRotation += mouseMovement.X * elapsedMs * .01f;

                CameraHorizontalRotation += mouseMovement.Y * elapsedMs * -.01f;
                CameraHorizontalRotation = MathHelper.Clamp(CameraHorizontalRotation, -90, 90);

                Mouse.SetMousePosition(center);
            }

            Vector2 cameraPosition = new Vector2(CameraData.X, CameraData.Z);

            bool forwards = KeyConfig.MoveCameraForwardBinding.IsAnyDown();
            bool backwards = KeyConfig.MoveCameraBackwardBinding.IsAnyDown();
            bool left = KeyConfig.MoveCameraLeftBinding.IsAnyDown();
            bool right = KeyConfig.MoveCameraRightBinding.IsAnyDown();

            if (forwards || backwards)
            {
                cameraPosition += MathHelper.PointFromAngle(forwards ? CameraVerticalRotation : CameraVerticalRotation - 180f, cameraDistance);
            }

            if (left || right)
            {
                cameraPosition += MathHelper.PointFromAngle(CameraVerticalRotation + (right ? +90f : -90f), cameraDistance);
            }

            bool up = KeyConfig.MoveCameraUpBinding.IsAnyDown();
            bool down = KeyConfig.MoveCameraDownBinding.IsAnyDown();

            if (up || down)
            {
                CameraData.Height += elapsedMs * (fastCamera ? FastCameraSpeed : CameraSpeed) * .5f * (up ? +1f : -1f);
            }

            if (KeyConfig.ResetCameraFovBinding.IsAnyTapped())
                CameraData.FieldOfView = DEFAULT_FOV;

            bool increaseFov = KeyConfig.IncreaseCameraFovBinding.IsAnyDown();
            bool decreaseFov = KeyConfig.DecreaseCameraFovBinding.IsAnyDown();

            if (increaseFov || decreaseFov)
            {
                CameraData.FieldOfView += elapsedMs * .25f * (increaseFov ? +1f : -1f);
                CameraData.FieldOfView = MathHelper.Clamp(CameraData.FieldOfView, MIN_FOV, MAX_FOV);
            }

            CameraData.X = cameraPosition.X;
            CameraData.Z = cameraPosition.Y;

            Vector2 focus = cameraPosition + MathHelper.PointFromAngle(CameraVerticalRotation, 1f);
            CameraData.FocusX = focus.X;
            CameraData.FocusZ = focus.Y;
            
            CameraData.FocusHeight = CameraData.Height + MathHelper.PointFromAngle(CameraHorizontalRotation, 1f).X;

            CameraData.Slant = CameraSlant;

            WriteDivaCameraData();
        }
    }
}
