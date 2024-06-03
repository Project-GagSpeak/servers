using GagspeakServer.Utils.Configuration;

namespace GagspeakServer.Services;

public interface IConfigService<T> where T : class, IGagspeakConfig
{
    bool IsMain { get; }
    T1 GetValue<T1>(string key);
    T1 GetValueOrDefault<T1>(string key, T1 defaultValue);
    string ToString();
}