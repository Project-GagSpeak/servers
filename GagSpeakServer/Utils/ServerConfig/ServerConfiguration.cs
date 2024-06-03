using System.Text;

namespace GagspeakServer.Utils.Configuration;

public class ServerConfiguration : GagspeakConfigBase
{
    [RemoteConfig]
    public Uri CdnFullUrl { get; set; } = null;

    [RemoteConfig]
    public Version ExpectedClientVersion { get; set; } = new Version(0, 0, 0);

    [RemoteConfig]
    public bool PurgeUnusedAccounts { get; set; } = false;

    [RemoteConfig]
    public int PurgeUnusedAccountsPeriodInDays { get; set; } = 14;
    public string GeoIPDbCityFile { get; set; } = string.Empty;

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(CdnFullUrl)} => {CdnFullUrl}");
        sb.AppendLine($"{nameof(ExpectedClientVersion)} => {ExpectedClientVersion}");
        sb.AppendLine($"{nameof(PurgeUnusedAccounts)} => {PurgeUnusedAccounts}");
        sb.AppendLine($"{nameof(PurgeUnusedAccountsPeriodInDays)} => {PurgeUnusedAccountsPeriodInDays}");
        sb.AppendLine($"{nameof(GeoIPDbCityFile)} => {GeoIPDbCityFile}");
        return sb.ToString();
    }
}