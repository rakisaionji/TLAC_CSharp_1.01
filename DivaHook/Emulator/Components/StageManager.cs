using System;
using DivaHook.Emulator.Config;

namespace DivaHook.Emulator.Components
{
    public class StageManager : IEmulatorComponent
    {
        private const long PV_LEVEL_INFO_STRUCT_ADDRESS = 0x0000000141197E00L;

        private const long PLAYS_PER_SESSION_GETTER_ADDRESS = 0x000000014038AEE0L;

        public const int PLAYS_PER_SESSION = 0x39393939;

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public StageManager(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
            // override 通常モード per session play count
            {
                byte[] plays = BitConverter.GetBytes(PLAYS_PER_SESSION);

                MemoryManipulator.Write(
                    PLAYS_PER_SESSION_GETTER_ADDRESS + 0xA9L, // wait for arguments to be popped then jmp to return loc
                    new byte[]
                    {
                        // mov eax, PLAYS_PER_SESSION
                        0xB8, plays[0], plays[1], plays[2], plays[3],
                        // jmp 0x14038AFB7 
                        0xEB, 0x27
                    });

                MemoryManipulator.Write(
                    PLAYS_PER_SESSION_GETTER_ADDRESS + 0xD7L,
                    Assembly.GetNopInstructions(2)
                    );
            }

            return;
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            //MemoryManipulator.WriteInt32(GetStageInfoAddress(), 0x2);

            MemoryManipulator.WriteByte(GetPvContinue(), 0x1);
        }

        private long GetPvContinue()
        {
            // unknown object pointer argument passed in rcx to the StartGameOverMain function
            return 0x0000000141197AD0L + 0x8C;
        }

        private long GetDifficultyAddress()
        {
            return PV_LEVEL_INFO_STRUCT_ADDRESS + 0x0L;
        }

        private long GetDifficultyEditionAddress()
        {
            return PV_LEVEL_INFO_STRUCT_ADDRESS + 0x4L;
        }

        private long GetStageInfoAddress()
        {
            return PV_LEVEL_INFO_STRUCT_ADDRESS + 0x8L;
        }
    }
}
