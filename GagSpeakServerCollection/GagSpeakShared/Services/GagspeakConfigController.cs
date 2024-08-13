using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GagspeakShared.Services;

[Route("configuration/[controller]")]
[Authorize(Policy = "Internal")]
public class GagspeakConfigController<T> : Controller where T : class, IGagspeakConfiguration
{
    private readonly ILogger<GagspeakConfigController<T>> _logger;
    private IOptionsMonitor<T> _config;

    public GagspeakConfigController(IOptionsMonitor<T> config, ILogger<GagspeakConfigController<T>> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet("GetConfigurationEntry")]
    [Authorize(Policy = "Internal")]
    public IActionResult GetConfigurationEntry(string key, string defaultValue)
    {
        var result = _config.CurrentValue.SerializeValue(key, defaultValue);
        _logger.LogInformation("Requested " + key + ", returning:" + result);
        return Ok(result);
    }
}

#pragma warning disable MA0048 // File name must match type name

/// <summary> The controller for the base configuration </summary>
public class GagspeakBaseConfigurationController : GagspeakConfigController<GagspeakConfigurationBase>
{
    public GagspeakBaseConfigurationController(IOptionsMonitor<GagspeakConfigurationBase> config, ILogger<GagspeakBaseConfigurationController> logger) : base(config, logger)
    {
    }
}

/// <summary> The controller for the server configuration </summary>
public class GagspeakServerConfigurationController : GagspeakConfigController<ServerConfiguration>
{
    public GagspeakServerConfigurationController(IOptionsMonitor<ServerConfiguration> config, ILogger<GagspeakServerConfigurationController> logger) : base(config, logger)
    {
    }
}

/// <summary> The controller for the discord configuration </summary>
public class GagspeakDiscordConfigurationController : GagspeakConfigController<DiscordConfiguration>
{
    public GagspeakDiscordConfigurationController(IOptionsMonitor<DiscordConfiguration> config, ILogger<GagspeakDiscordConfigurationController> logger) : base(config, logger)
    {
    }
}
#pragma warning restore MA0048 // File name must match type name
