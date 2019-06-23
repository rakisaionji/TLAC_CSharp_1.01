using DivaHook.Emulator.Config;
using System;

namespace DivaHook.Emulator.Components
{
    public class CoinEmulator : IEmulatorComponent
    {
        private const long COIN_COUNT_ADDRESS = 0x0000000141027F48L;

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
