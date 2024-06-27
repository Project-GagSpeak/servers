using System.Text;

namespace GagspeakShared.Utils.Configuration;

public class AuthServiceConfiguration : GagspeakConfigurationBase
{
    public int FailedAuthForTempBan { get; set; } = 20; // making it 100 temporarily so i dont accidently tempban myself while testing lol.
    public int TempBanDurationInMinutes { get; set; } = 1;
    public List<string> WhitelistedIps { get; set; } = new();

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(RedisPool)} => {RedisPool}");
        return sb.ToString();
    }
}
