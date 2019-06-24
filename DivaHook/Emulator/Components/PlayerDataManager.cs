using DivaHook.Emulator.Config;
using System;
using System.Text;

namespace DivaHook.Emulator.Components
{
    public class PlayerDataManager : IEmulatorComponent
    {
        private const long PLAYER_DATA_ADDRESS = 0x0000000140E6E9B0L;
        private const long PLAYER_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x0E0L;
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_SKIN_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x548L;
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_VP_ADDRESS = PLAYER_DATA_ADDRESS + 0x12CL;
        private const long PLAYER_HP_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x130L;
        private const long PLAYER_ACT_TOGGLE_ADDRESS = PLAYER_DATA_ADDRESS + 0x134L;
        private const long PLAYER_ACT_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x138L;
        private const long PLAYER_ACT_SLVOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x13CL;
        private const long PLAYER_PV_SORT_KIND_ADDRESS = PLAYER_DATA_ADDRESS + 0x494L;
        private const long PLAYER_PWD_STAT_ADDRESS = PLAYER_DATA_ADDRESS + 0x560L;
        private const long PLAYER_CLEAR_BORDER_ADDRESS = PLAYER_DATA_ADDRESS + 0x95CL; // clear_border_disp_bit
        private const long PLAYER_RANK_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0x9A4L; // interim_ranking_disp_flag

        private const long SET_DEFAULT_PLAYER_DATA_ADDRESS = 0x000000014033F5F0L;
        private const long MODSELECTOR_SET_MODULE_ADDRESS = 0x00000001403F27CEL;
        private const long MODSELECTOR_SET_ITEM_A_ADDRESS = 0x00000001403F2BC4L;
        private const long MODSELECTOR_SET_ITEM_B_ADDRESS = 0x00000001403F3AA3L;
        private const long MODSELECTOR_SET_ITEM_C_ADDRESS = 0x00000001403F3594L;
        private const long MODSELECTOR_SET_ITEM_D_ADDRESS = 0x00000001403F3842L;
        private const long MODSELECTOR_MISC_CHECK_ADDRESS = 0x00000001403F2332L;
        private const long MODSELECTOR_CLOSE_AFTER_MODULE = 0x00000001403F3F99L;
        private const long MODSELECTOR_CLOSE_AFTER_CUSTOMIZE = 0x00000001403F3E53L;

        private const long MODULE_TABLE_START = 0x0000000140E6EAF0L;
        private const long MODULE_TABLE_END = 0x0000000140E6EB6FL;
        private const long ITEM_TABLE_START = 0x0000000140E6EC40L;
        private const long ITEM_TABLE_END = 0x0000000140E6ECBFL;

        private const long CURRENT_SUB_STATE = 0x0000000140CEFABCL;

        // private const string DefaultName = "ＮＯ－ＮＡＭＥ";
        private byte[] PlayerNameValue;
        private int PlayerNameAddress;
        private int Level = 1;
        private int PlateId = 0;
        private int PlateEff = -1;
        private int VocaloidPoint = 0;
        private int SkinEquip = 0;
        private byte ActToggle = 1;
        private int ActVol = 100;
        private int ActSlideVol = 100;
        private int HpVol = 100;
        private int PasswordStatus = -1;
        private int PvSortKind = 2;

        private int step = 0;

        public KeyConfig KeyConfig { get; private set; }
        public PlayerConfig PlayerConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public PlayerDataManager(MemoryManipulator memoryManipulator, PlayerConfig playerConfig)
        {
            MemoryManipulator = memoryManipulator;
            PlayerConfig = playerConfig;
        }

        private void SetPlayerConfig(ref string field1, string field2)
        {
            if (field1.StartsWith("*"))
                field1 = field1.Substring(1);
            else
                field1 = field2;
        }

        private void InjectPatches()
        {
            // Prevent the PlayerData from being reset so we don't need to keep updating the PlayerData struct
            MemoryManipulator.WritePatch(SET_DEFAULT_PLAYER_DATA_ADDRESS, new byte[] { 0xC3 }); // ret
            // Allow player to select the module
            MemoryManipulator.WritePatch(MODSELECTOR_SET_MODULE_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_SET_MODULE_ADDRESS + 0x2L, 0x34); // nop
            // Allow player to select extra item
            MemoryManipulator.WritePatch(MODSELECTOR_SET_ITEM_A_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_SET_ITEM_A_ADDRESS + 0x2L, 0x32); // nop
            MemoryManipulator.WritePatch(MODSELECTOR_SET_ITEM_B_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_SET_ITEM_B_ADDRESS + 0x2L, 0x32); // nop
            MemoryManipulator.WritePatch(MODSELECTOR_SET_ITEM_C_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_SET_ITEM_C_ADDRESS + 0x2L, 0x35); // nop
            MemoryManipulator.WritePatch(MODSELECTOR_SET_ITEM_D_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_SET_ITEM_D_ADDRESS + 0x2L, 0x34); // nop
            // Some miscellaneous check
            MemoryManipulator.WritePatch(MODSELECTOR_MISC_CHECK_ADDRESS, new byte[] { 0xB0, 0x01 }); // test al, al --> mov al, 1
            MemoryManipulator.WritePatchNop(MODSELECTOR_MISC_CHECK_ADDRESS + 0x2L, 0x32); // nop
            // Fix annoying behavior of closing after changing module or item
            MemoryManipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_MODULE, new byte[] { 0x85 }); // je --> jne
            MemoryManipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_CUSTOMIZE, new byte[] { 0x85 }); // je --> jne
        }

        public void InitializeDivaMemory()
        {
            InjectPatches();
            var AppConfig = Properties.Settings.Default;
            if (AppConfig.PlayerName.Equals(PlayerConfig.PlayerName))
            {
                SetPlayerConfig(ref PlayerConfig.VocaloidPoint, AppConfig.VocaloidPoint);
                SetPlayerConfig(ref PlayerConfig.ActToggle, AppConfig.ActToggle);
                SetPlayerConfig(ref PlayerConfig.ActVol, AppConfig.ActVol);
                SetPlayerConfig(ref PlayerConfig.ActSlideVol, AppConfig.ActSlideVol);
                SetPlayerConfig(ref PlayerConfig.PvSortKind, AppConfig.PvSortKind);
            }
            else
            {
                AppConfig.PlayerName = PlayerConfig.PlayerName;
                AppConfig.VocaloidPoint = PlayerConfig.VocaloidPoint;
                AppConfig.ActToggle = PlayerConfig.ActToggle;
                AppConfig.ActVol = PlayerConfig.ActVol;
                AppConfig.ActSlideVol = PlayerConfig.ActSlideVol;
                AppConfig.HpVol = PlayerConfig.HpVol;
                AppConfig.PvSortKind = PlayerConfig.PvSortKind;
            }
            PlayerNameValue = new byte[21];
            var b_name = Encoding.UTF8.GetBytes(PlayerConfig.PlayerName);
            Buffer.BlockCopy(b_name, 0, PlayerNameValue, 0, b_name.Length);
            PlayerNameAddress = MemoryManipulator.ReadInt32(PLAYER_NAME_ADDRESS);
            ReadPlayerData();
            if (Level < 1) Level = 1;
            if (ActVol < 0 || ActVol > 100) ActVol = 100;
            if (HpVol < 0 || HpVol > 100) HpVol = 100;
            // use_card = 1 // Required to allow for module selection
            MemoryManipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            // Allow player to select the module and extra items (by vladkorotnev)
            for (long i = MODULE_TABLE_START; i <= MODULE_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
            for (long i = ITEM_TABLE_START; i <= ITEM_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
            // Display interim rank (despite it is not yet fully functional)
            MemoryManipulator.WriteByte(PLAYER_RANK_DISP_ADDRESS, 1);
            // Display HARD and EXCELLENT clear border, I'm lazy so it's hardcoded
            MemoryManipulator.WriteByte(PLAYER_CLEAR_BORDER_ADDRESS, 3);
            WritePlayerData();
        }

        private void ReadPlayerData()
        {
            int.TryParse(PlayerConfig.Level, out Level);
            int.TryParse(PlayerConfig.SkinEquip, out SkinEquip);
            int.TryParse(PlayerConfig.PlateId, out PlateId);
            int.TryParse(PlayerConfig.PlateEff, out PlateEff);
            int.TryParse(PlayerConfig.VocaloidPoint, out VocaloidPoint);
            byte.TryParse(PlayerConfig.ActToggle, out ActToggle);
            int.TryParse(PlayerConfig.ActVol, out ActVol);
            int.TryParse(PlayerConfig.ActSlideVol, out ActSlideVol);
            int.TryParse(PlayerConfig.HpVol, out HpVol);
            int.TryParse(PlayerConfig.PasswordStatus, out PasswordStatus);
            int.TryParse(PlayerConfig.PvSortKind, out PvSortKind);
        }

        private void WritePlayerData()
        {
            var b = MemoryManipulator.Read(PlayerNameAddress, 21);
            MemoryManipulator.Write(PlayerNameAddress, PlayerNameValue);
            MemoryManipulator.WriteInt32(PLAYER_SKIN_EQUIP_ADDRESS, SkinEquip);
            MemoryManipulator.WriteInt32(PLAYER_LEVEL_ADDRESS, Level);
            MemoryManipulator.WriteInt32(PLAYER_PLATE_ID_ADDRESS, PlateId);
            MemoryManipulator.WriteInt32(PLAYER_PLATE_EFF_ADDRESS, PlateEff);
            MemoryManipulator.WriteInt32(PLAYER_VP_ADDRESS, VocaloidPoint);
            MemoryManipulator.WriteByte(PLAYER_ACT_TOGGLE_ADDRESS, ActToggle);
            MemoryManipulator.WriteInt32(PLAYER_ACT_VOL_ADDRESS, ActVol);
            MemoryManipulator.WriteInt32(PLAYER_ACT_SLVOL_ADDRESS, ActSlideVol);
            MemoryManipulator.WriteInt32(PLAYER_HP_VOL_ADDRESS, HpVol);
            MemoryManipulator.WriteInt32(PLAYER_PWD_STAT_ADDRESS, PasswordStatus);
            MemoryManipulator.WriteInt32(PLAYER_PV_SORT_KIND_ADDRESS, PvSortKind);
        }

        private void SavePlayerData()
        {
            // MemoryManipulator.WriteInt32(GetPlayerNameFAddress(), 0x10);
            VocaloidPoint = MemoryManipulator.ReadInt32(PLAYER_VP_ADDRESS);
            ActToggle = MemoryManipulator.ReadByte(PLAYER_ACT_TOGGLE_ADDRESS);
            ActVol = MemoryManipulator.ReadInt32(PLAYER_ACT_VOL_ADDRESS); ;
            ActSlideVol = MemoryManipulator.ReadInt32(PLAYER_ACT_SLVOL_ADDRESS);
            HpVol = MemoryManipulator.ReadInt32(PLAYER_HP_VOL_ADDRESS);
            PvSortKind = MemoryManipulator.ReadInt32(PLAYER_PV_SORT_KIND_ADDRESS);
            var sett = Properties.Settings.Default;
            sett.VocaloidPoint = VocaloidPoint.ToString();
            sett.ActToggle = ActToggle.ToString();
            sett.ActVol = ActVol.ToString();
            sett.ActSlideVol = ActSlideVol.ToString();
            sett.HpVol = HpVol.ToString();
            sett.PvSortKind = PvSortKind.ToString();
            sett.Save();
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            var currentSubState = (SubGameState)MemoryManipulator.ReadInt32(CURRENT_SUB_STATE);
            switch (currentSubState)
            {
                case SubGameState.SUB_LOGO:
                    if (step == 3) SavePlayerData();
                    step = 0;
                    break;
                case SubGameState.SUB_SELECTOR:
                case SubGameState.SUB_GAME_SEL:
                    if (step == 3) { SavePlayerData(); step = 0; }
                    if (step == 0) WritePlayerData();
                    step = 1;
                    break;
                case SubGameState.SUB_GAME_MAIN:
                    step = 2;
                    break;
                case SubGameState.SUB_STAGE_RESULT:
                    step = 3;
                    break;
                default:
                    break;
            }
        }
    }
}
