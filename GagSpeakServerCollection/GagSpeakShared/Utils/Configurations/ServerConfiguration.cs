using System.Text;

namespace GagspeakShared.Utils.Configuration;

/// <summary> Config for the server </summary>
public class ServerConfiguration : GagspeakConfigurationBase
{
    [RemoteConfig]
    public Version ExpectedClientVersion { get; set; } = new Version(0, 0, 0);

    [RemoteConfig]
    public bool PurgeUnusedAccounts { get; set; } = false;

    [RemoteConfig]
    public int PurgeUnusedAccountsPeriodInDays { get; set; } = 14;

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(ExpectedClientVersion)} => {ExpectedClientVersion}");
        sb.AppendLine($"{nameof(PurgeUnusedAccounts)} => {PurgeUnusedAccounts}");
        sb.AppendLine($"{nameof(PurgeUnusedAccountsPeriodInDays)} => {PurgeUnusedAccountsPeriodInDays}");
        return sb.ToString();
    }
}