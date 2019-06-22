using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Camera
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraData
    {
        public static readonly int BYTE_SIZE = Marshal.SizeOf(typeof(CameraData));

        public float X;
        public float Height;
        public float Z;

        public float FocusX;
        public float FocusHeight;
        public float FocusZ;

        public float Slant;
        public float FieldOfView;

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[BYTE_SIZE];
            GCHandle pinStructure = GCHandle.Alloc(this, GCHandleType.Pinned);

            try
            {
                Marshal.Copy(pinStructure.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                pinStructure.Free();
            }
        }

        public static CameraData FromBytes(byte[] bytes)
        {
            IntPtr ptr = Marshal.AllocHGlobal(BYTE_SIZE);

            Marshal.Copy(bytes, 0, ptr, BYTE_SIZE);

            CameraData camera = (CameraData)Marshal.PtrToStructure(ptr, typeof(CameraData));

            Marshal.FreeHGlobal(ptr);

            return camera;
        }
    }
}
