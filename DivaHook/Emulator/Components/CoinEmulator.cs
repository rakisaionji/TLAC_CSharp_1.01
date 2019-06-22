using System;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator.Components
{
    public class CoinEmulator : IEmulatorComponent
    {
        private const long COIN_COUNT_ADDRESS = 0x000000014CD93788L;

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public CoinEmulator(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
            return;
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            if (KeyConfig.InsertCoinBinding.IsAnyTapped())
            {
                byte oldCoinTotal = MemoryManipulator.ReadByte(COIN_COUNT_ADDRESS);
                byte coinTotal = (byte)(Math.Min(oldCoinTotal + 0x1, byte.MaxValue));

                MemoryManipulator.WriteByte(COIN_COUNT_ADDRESS, coinTotal);
            }
        }
    }
}
