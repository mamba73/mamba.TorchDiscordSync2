using System;
using System.IO;
using System.Xml.Serialization;

namespace mamba.TorchDiscordSync.Config
{
    [Serializable]
    public class MainConfig
    {
        [XmlElement]
        public string BotToken { get; set; } = "YOUR_DISCORD_BOT_TOKEN";

        [XmlElement]
        public ulong GuildID { get; set; } = 0;

        private static string ConfigPath
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var instanceDir = Path.Combine(baseDir, "Instance");
                var mambaDir = Path.Combine(instanceDir, "mambaTorchDiscordSync");
                return Path.Combine(mambaDir, "MainConfig.cfg");
            }
        }

        public static MainConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new MainConfig();
                defaultConfig.Save();
                return defaultConfig;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(MainConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
            {
                return (MainConfig)serializer.Deserialize(fs);
            }
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MainConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }
}