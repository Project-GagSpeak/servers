namespace GagspeakServer.Utils.Configuration;

public interface IGagspeakConfig
{
    T GetValueOrDefault<T>(string key, T defaultValue);
    T GetValue<T>(string key);
    string SerializeValue(string key, string defaultValue);
}
