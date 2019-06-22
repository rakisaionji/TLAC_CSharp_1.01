using System;

namespace DivaHook.Emulator.Components
{
    [Flags]
    public enum JvsButtons : uint
    {
        JVS_NONE = 0 << 0,

        JVS_TEST = 1 << 0,
        JVS_SERVICE = 1 << 1,
        JVS_START = 1 << 2,

        JVS_TRIANGLE = 1 << 7,
        JVS_SQUARE = 1 << 8,
        JVS_CROSS = 1 << 9,
        JVS_CIRCLE = 1 << 10,

        JVS_SW1 = 1 << 18,
        JVS_SW2 = 1 << 19,
    }
}
