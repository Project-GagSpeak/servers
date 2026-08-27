using GagspeakAPI.Profiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GagspeakShared.Utils;
#nullable enable

/// <summary>
///   Extensions for default templates and theme styles.
/// </summary>
public static class ProfilesJsonEx
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new PrimitiveShapeConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    ///   Retrieves the UserProfileV1 from a JSON string.
    /// </summary>
    public static UserProfileV1 ReadUserV1(string? jsonData)
    {
        jsonData = (jsonData ?? string.Empty).Trim();
        if (jsonData.StartsWith("{", StringComparison.Ordinal))
            return JsonConvert.DeserializeObject<UserProfileV1>(jsonData, Settings) ?? new UserProfileV1();
        return new UserProfileV1 { Version = 1, Description = jsonData };
    }

    /// <summary>
    ///   Serializes the UserProfileV1 to a JSON string.
    /// </summary>
    public static string WriteToJson(this UserProfileV1 profileV1)
        => JsonConvert.SerializeObject(profileV1 ?? new UserProfileV1(), Formatting.None, Settings);

    // Revise the below later.
    public static UserProfileTheme ReadThemeFromJToken(JToken? themeToken)
    {
        if (themeToken == null)
            return new UserProfileTheme();
        // If this fails, it will bubble up to the caller
        return themeToken.ToObject<UserProfileTheme>(JsonSerializer.Create(Settings)) ?? new UserProfileTheme();
    }

    public static string WriteThemeToJson(UserProfileTheme theme)
        => JsonConvert.SerializeObject(theme, Formatting.None, Settings);
}
#nullable disable
