namespace GagspeakShared.Utils.Configuration;

public interface IGagspeakConfiguration
{
    T GetValueOrDefault<T>(string key, T defaultValue);
    T GetValue<T>(string key);
    string SerializeValue(string key, string defaultValue);
}
