using System.Text;

namespace GagspeakShared.Utils.Configuration;

public class DiscordConfiguration : GagspeakConfigurationBase
{
    public string DiscordBotToken { get; set; } = string.Empty;     // the discord bot token
    public ulong? DiscordChannelForMessages { get; set; } = null;   // the discord channel for messages
    public ulong? DiscordChannelForReports { get; set; } = null;    // the discord channel for reports
    public ulong? DiscordChannelForCommands { get; set; } = null;   // the discord channel for commands
    public Dictionary<ulong, string> VanityRoles { get; set; } = new Dictionary<ulong, string>(); // the vanity roles

    /// <summary>
    /// This tostring method will output all information of the discords configuration to a string return.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(DiscordBotToken)} => {DiscordBotToken}");
        sb.AppendLine($"{nameof(MainServerAddress)} => {MainServerAddress}");
        sb.AppendLine($"{nameof(DiscordChannelForMessages)} => {DiscordChannelForMessages}");
        sb.AppendLine($"{nameof(DiscordChannelForReports)} => {DiscordChannelForReports}");
        sb.AppendLine($"{nameof(DiscordChannelForCommands)} => {DiscordChannelForCommands}");
        foreach (var role in VanityRoles)
        {
            sb.AppendLine($"{nameof(VanityRoles)} => {role.Key} = {role.Value}");
        }
        return sb.ToString();
    }
}