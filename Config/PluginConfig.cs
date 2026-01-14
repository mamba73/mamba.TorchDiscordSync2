using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace mamba.TorchDiscordSync.Config
{
    [Serializable]
    public class PluginConfig
    {
        [XmlElement]
        public string DiscordToken { get; set; }

        [XmlElement]
        public ulong GuildID { get; set; }

        [XmlElement]
        public ulong CategoryID { get; set; }

        [XmlElement]
        public ulong StaffChannelStatus { get; set; }

        [XmlElement]
        public ulong StaffChannelLog { get; set; }

        [XmlElement]
        public ulong StaffChannelServerStatus { get; set; }

        [XmlElement]
        public ulong EventChannelDeathJoinLeave { get; set; }

        [XmlElement]
        public int SyncIntervalSeconds { get; set; }

        [XmlElement]
        public bool Debug { get; set; }

        [XmlElement]
        public float SimSpeedThreshold { get; set; } = 0.8f;

        [XmlArray("AdminSteamIDs")]
        [XmlArrayItem("SteamID")]
        public List<string> AdminSteamIDs { get; set; } = new List<string>();

        private static string ConfigDir
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var instanceDir = Path.Combine(baseDir, "Instance");
                if (!Directory.Exists(instanceDir))
                    Directory.CreateDirectory(instanceDir);

                var mambaDir = Path.Combine(instanceDir, "mambaTorchDiscordSync");
                if (!Directory.Exists(mambaDir))
                    Directory.CreateDirectory(mambaDir);

                return mambaDir;
            }
        }

        private static string ConfigPath => Path.Combine(ConfigDir, "MambaTorchDiscordSync.cfg");

        public static PluginConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new PluginConfig
                {
                    DiscordToken = "YOUR_DISCORD_BOT_TOKEN",
                    GuildID = 0,
                    CategoryID = 0,
                    StaffChannelStatus = 0,
                    StaffChannelLog = 0,
                    StaffChannelServerStatus = 0,
                    EventChannelDeathJoinLeave = 0,
                    SyncIntervalSeconds = 60,
                    Debug = true,
                    SimSpeedThreshold = 0.8f,
                    AdminSteamIDs = new List<string> { "76561198000000000" }
                };

                defaultConfig.Save();
                return defaultConfig;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
            {
                return (PluginConfig)serializer.Deserialize(fs);
            }
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PluginConfig));
            using (FileStream fs = new FileStream(ConfigPath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }
}