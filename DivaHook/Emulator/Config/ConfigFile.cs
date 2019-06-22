using System;
using System.Text;
using System.IO;

namespace DivaHook.Emulator.Config
{
    public abstract class ConfigFile
    {
        public abstract string ConfigFileName { get; }

        protected readonly char[] KeyValueSeperator = new char[] { '=' };

        protected readonly char ValueSeperator = ',';

        public virtual void TryLoadConfig()
        {
            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string configPath = Path.Combine(directory, ConfigFileName);

            if (File.Exists(configPath))
            {
                LoadFromFile(configPath);
            }
            else
            {
                WriteToFile(configPath);
            }
        }

        public virtual void LoadFromFile(string filePath)
        {
            ParseLines(File.ReadAllLines(filePath));
        }

        public virtual void WriteToFile(string filePath)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"# {ConfigFileName}");

            FormatFile(builder);

            File.WriteAllText(filePath, builder.ToString().TrimEnd());
        }

        protected abstract void FormatFile(StringBuilder builder);

        protected virtual void ParseLines(string[] lines)
        {
            foreach (var line in lines)
            {
                if (IsLineComment(line))
                    continue;

                ParseLine(line);
            }
        }

        protected abstract void ParseLine(string line);

        protected static bool IsLineComment(string line)
        {
            return line.StartsWith("#") || line.StartsWith(";");
        }
    }
}
