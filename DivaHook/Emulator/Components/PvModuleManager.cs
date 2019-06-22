using DivaHook.Emulator.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace DivaHook.Emulator.Components
{
    class PvModuleManager : IEmulatorComponent
    {
        private const string MODULE_LIST_PATH = "PvCostume.dat";

        private const long PLAYER_MODULE_ADDRESS = 0x0000000140E6E9B0L + 0x1C0L;
        private const long SEL_PVID_BYFRAME_ADDRESS = 0x0000000140EA5B14L;
        private const long CURRENT_SUB_STATE = 0x0000000140CEFABCL;

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        private int lastPvId;
        private Dictionary<int, int[]> PvModules;

        public PvModuleManager(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        private void LoadPvModules()
        {
            try
            {
                PvModules = new Dictionary<int, int[]>();
                string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, MODULE_LIST_PATH);
                var dat = File.ReadAllText(path);
                var ln = dat.Split(';');
                foreach (var ll in ln)
                {
                    var l = ll.Split(',');
                    if (l.Length < 2) continue;
                    var n = l.Length - 1;
                    var p = int.Parse(l[0]);
                    var m = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        m[i] = int.Parse(l[i + 1]);
                    }
                    PvModules.Add(p, m);
                }
            }
            catch (Exception)
            {
                PvModules = null;
            }
        }

        public void InitializeDivaMemory()
        {
            lastPvId = 0;
            LoadPvModules();
        }
        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            if (PvModules == null) return;
            var currentSubState = (SubGameState)MemoryManipulator.ReadInt32(CURRENT_SUB_STATE);
            if (currentSubState == SubGameState.SUB_SELECTOR || currentSubState == SubGameState.SUB_GAME_SEL)
            {
                var pvId = MemoryManipulator.ReadInt32(SEL_PVID_BYFRAME_ADDRESS);
                if (pvId == lastPvId || pvId == -1) return;
                if (!PvModules.ContainsKey(pvId)) return;
                var pvMd = PvModules[pvId];
                if (pvMd == null) return;
                var i = 0;
                foreach (var md in pvMd)
                {
                    var addr = PLAYER_MODULE_ADDRESS + (i * 0x4L);
                    MemoryManipulator.WriteInt32(addr, md); i++;
                }
                while (i <= 6)
                {
                    var addr = PLAYER_MODULE_ADDRESS + (i * 0x4L);
                    MemoryManipulator.WriteInt32(addr, 0); i++;
                }
                lastPvId = pvId;
            }
        }
    }
}
