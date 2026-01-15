using System;

namespace mamba.TorchDiscordSync.Models
{
    /// <summary>
    /// Defines command properties and authorization level
    /// </summary>
    [Serializable]
    public class CommandModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
        public bool RequiresAdmin { get; set; }
        public int MinimumArguments { get; set; }

        public CommandModel() { }

        public CommandModel(string name, string description, string usage, bool requiresAdmin, int minArgs)
        {
            Name = name;
            Description = description;
            Usage = usage;
            RequiresAdmin = requiresAdmin;
            MinimumArguments = minArgs;
        }

        public string GetHelpText()
        {
            return $"{Usage}\n  └─ {Description}";
        }
    }
}