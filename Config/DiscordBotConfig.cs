using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace mamba.TorchDiscordSync.Config
{
    [Serializable]
    public class DiscordBotConfig
    {
        [XmlElement]
        public string BotToken { get; set; }

        [XmlElement]
        public ulong GuildID { get; set; }

        [XmlElement]
        public string BotPrefix { get; set; } = "!";

        [XmlElement]
        public bool EnableDMNotifications { get; set; } = true;

        [XmlElement]
        public int VerificationCodeExpirationMinutes { get; set; } = 15;

        private static string ConfigPath
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var instanceDir = Path.Combine(baseDir, "Instance");
                var mambaDir = Path.Combine(instanceDir, "mambaTorchDiscordSync");
                return Path.Combine(mambaDir, "DiscordBotConfig.cfg");
            }
        }

        public static DiscordBotConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new DiscordBotConfig
                {
                    BotToken = "YOUR_DISCORD_BOT_TOKEN",
                    GuildID = 0,
                    BotPrefix = "!",
                    EnableDMNotifications = true,
                    VerificationCodeExpirationMinutes = 15
                };

                defaultConfig.Save();
                return defaultConfig;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(DiscordBotConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
            {
                return (DiscordBotConfig)serializer.Deserialize(fs);
            }
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DiscordBotConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }
}