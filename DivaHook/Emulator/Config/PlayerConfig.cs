using System;
using System.Text;
using System.Collections.Generic;

namespace DivaHook.Emulator.Config
{
    public sealed class PlayerConfig : ConfigFile
    {
        public override string ConfigFileName => "PlayerConfig.ini";

        public string PlayerName = "ＮＯ－ＮＡＭＥ";
        public string Level = "1";
        public string PlateId = "0";
        public string PlateEff = "-1";
        public string VocaloidPoint = "0";
        public string SkinEquip = "0";
        public string ActToggle = "1";
        public string ActVol = "100";
        public string ActSlideVol = "100";
        public string HpVol = "100";
        public string PasswordStatus = "-1";
        public string PvSortKind = "2";

        private readonly Dictionary<string, StringBindingAccessor> accessorDictionary = null;

        public PlayerConfig()
        {
            accessorDictionary = GetAccessorDictionary();
        }

        protected override void ParseLine(string line)
        {
            string[] keyValuePair = line.Split(KeyValueSeperator, 2);

            if (keyValuePair.Length < 2)
                return;

            string keyString = keyValuePair[0].Trim();

            if (accessorDictionary.TryGetValue(keyString, out var accessor))
            {
                accessor.Setter?.Invoke(keyValuePair[1].Trim());
            }
        }

        protected override void FormatFile(StringBuilder builder)
        {
            string valueSeperator = $"{ValueSeperator} ";

            foreach (var entry in accessorDictionary)
            {
                builder.AppendLine($"{entry.Key} {KeyValueSeperator[0]} {string.Join(valueSeperator, entry.Value.Getter?.Invoke())}");
            }
        }

        private Dictionary<string, StringBindingAccessor> GetAccessorDictionary()
        {
            return new Dictionary<string, StringBindingAccessor>()
            {
                {
                    "player_name", new StringBindingAccessor(() => PlayerName, (v) => PlayerName = v)
                },
                {
                    "level", new StringBindingAccessor(() => Level, (v) => Level = v)
                },
                {
                    "level_plate_id", new StringBindingAccessor(() => PlateId, (v) => PlateId = v)
                },
                {
                    "level_plate_eff", new StringBindingAccessor(() => PlateEff, (v) => PlateEff = v)
                },
                {
                    "vocaloid_point", new StringBindingAccessor(() => VocaloidPoint, (v) => VocaloidPoint = v)
                },
                {
                    "skin_equip", new StringBindingAccessor(() => SkinEquip, (v) => SkinEquip = v)
                },
                {
                    "act_toggle", new StringBindingAccessor(() => ActToggle, (v) => ActToggle = v)
                },
                {
                    "act_vol", new StringBindingAccessor(() => ActVol, (v) => ActVol = v)
                },
                {
                    "act_slide_vol", new StringBindingAccessor(() => ActSlideVol, (v) => ActSlideVol = v)
                },
                {
                    "hp_vol", new StringBindingAccessor(() => HpVol, (v) => HpVol = v)
                },
                {
                    "password_status", new StringBindingAccessor(() => PasswordStatus, (v) => PasswordStatus = v)
                },
                {
                    "pv_sort_kind", new StringBindingAccessor(() => PvSortKind, (v) => PvSortKind = v)
                },
            };
        }
    }
}
