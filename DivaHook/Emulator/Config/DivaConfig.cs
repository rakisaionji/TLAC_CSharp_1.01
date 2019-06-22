using System;
using System.Text;
using System.Collections.Generic;

namespace DivaHook.Emulator.Config
{
    public sealed class DivaConfig : ConfigFile
    {
        public override string ConfigFileName => "DivaConfig.ini";

        public string ExePath = @"Y:\SBZV\diva.exe";

        public string ExeArguments = "-w";

        private readonly Dictionary<string, StringBindingAccessor> accessorDictionary = null;

        public DivaConfig()
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
                    "EXE_PATH", new StringBindingAccessor(() => ExePath, (v) => ExePath = v)
                },
                {
                    "ARGUMENTS", new StringBindingAccessor(() => ExeArguments, (v) => ExeArguments = v)
                },
            };
        }
    }
}
