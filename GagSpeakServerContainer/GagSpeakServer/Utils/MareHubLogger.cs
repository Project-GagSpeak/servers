using GagSpeakServer.Hubs;
using System.Runtime.CompilerServices;

namespace GagSpeakServer.Utils;

public class GagSpeakHubLogger
{
    private readonly GagSpeakHub _hub;
    private readonly ILogger<GagSpeakHub> _logger;

    public GagSpeakHubLogger(GagSpeakHub hub, ILogger<GagSpeakHub> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public static object[] Args(params object[] args)
    {
        return args;
    }

    public void LogCallInfo(object[] args = null, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogInformation("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
    }

    public void LogCallWarning(object[] args = null, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogWarning("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
    }
}
