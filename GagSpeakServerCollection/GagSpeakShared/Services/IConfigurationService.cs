using GagspeakShared.Utils.Configuration;

namespace GagspeakShared.Services;

public interface IConfigurationService<T> where T : class, IGagspeakConfiguration
{
    bool IsMain { get; }
    T1 GetValue<T1>(string key);
    T1 GetValueOrDefault<T1>(string key, T1 defaultValue);
    string ToString();
}