using GagspeakServer.Hubs;
using System.Runtime.CompilerServices;

// Ripped from the gagspeak hub logger as a way to reference, debug, and understand the process that the server undergoes for construction.

namespace GagspeakServer.Utils;

/// <summary>
/// Logger for the GagspeakHub
/// </summary>
public class GagspeakHubLogger
{
    private readonly GagspeakHub _hub;              // The GagspeakHub instance
    private readonly ILogger<GagspeakHub> _logger;  // The logger instance

    public GagspeakHubLogger(GagspeakHub hub, ILogger<GagspeakHub> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public ILogger Logger => _logger;

    public static object[] Args(params object[] args)
    {
        return args;
    }

    public void LogCallInfo(object[] args = null!, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogInformation("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
        //_logger.LogInformation("DEV UID:{method}{args}", methodName, formattedArgs);
    }

    public void LogCallWarning(object[] args = null!, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogWarning("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
        //_logger.LogWarning("DEV UID:{method}{args}", methodName, formattedArgs);
    }

    public void LogMessage(string message)
    {
        _logger.LogInformation("DEBUG MESSAGE: {message}", message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning("WARNING: {message}", message);
    }
}
