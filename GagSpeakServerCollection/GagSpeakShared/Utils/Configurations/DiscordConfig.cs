using System.Text;

namespace GagspeakShared.Utils.Configuration;

public class DiscordConfig : GagspeakConfigurationBase
{
    public string BotToken              { get; set; } = string.Empty;
    public ulong? ChannelForMessages    { get; set; } = null;
    public ulong? ChannelForReports     { get; set; } = null;
    public ulong? ChannelForResolutions { get; set; } = null;
    public ulong? ChannelForCommands    { get; set; } = null;

    public Dictionary<ulong, string> VanityRoles { get; set; } = new Dictionary<ulong, string>(); // the vanity roles

    /// <summary>
    /// This tostring method will output all information of the discords configuration to a string return.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(BotToken)} => {BotToken}");
        sb.AppendLine($"{nameof(MainServerAddress)} => {MainServerAddress}");
        sb.AppendLine($"{nameof(ChannelForMessages)} => {ChannelForMessages}");
        sb.AppendLine($"{nameof(ChannelForReports)} => {ChannelForReports}");
        sb.AppendLine($"{nameof(ChannelForResolutions)} => {ChannelForResolutions}");
        sb.AppendLine($"{nameof(ChannelForCommands)} => {ChannelForCommands}");
        foreach (var role in VanityRoles)
            sb.AppendLine($"{nameof(VanityRoles)} => {role.Key} = {role.Value}");
        return sb.ToString();
    }
}