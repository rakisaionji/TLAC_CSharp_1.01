using System;
using DivaHook.Emulator.Config;

namespace DivaHook.Emulator.Components
{
    public interface IEmulatorComponent
    {
        KeyConfig KeyConfig { get; }

        MemoryManipulator MemoryManipulator { get; }

        void InitializeDivaMemory();

        void UpdateEmulatorTick(TimeSpan deltaTime);
    }
}
