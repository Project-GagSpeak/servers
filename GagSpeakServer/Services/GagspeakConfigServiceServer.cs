using GagspeakServer.Utils;
using GagspeakServer.Utils.Configuration;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Text;

namespace GagspeakServer.Services;

// figure out wtf all this means later but for now just slap it under the hood.
public class GagspeakConfigServiceServer<T> : IConfigService<T> where T : class, IGagspeakConfig
{
    private readonly IOptionsMonitor<T> _config;
    public bool IsMain => true;

    public GagspeakConfigServiceServer(IOptionsMonitor<T> config)
    {
        _config = config;
    }

    public T1 GetValueOrDefault<T1>(string key, T1 defaultValue)
    {
        return _config.CurrentValue.GetValueOrDefault<T1>(key, defaultValue);
    }

    public T1 GetValue<T1>(string key)
    {
        return _config.CurrentValue.GetValue<T1>(key);
    }

    public override string ToString()
    {
        var props = _config.CurrentValue.GetType().GetProperties();
        StringBuilder sb = new();
        foreach (var prop in props)
        {
            var isRemote = prop.GetCustomAttributes(typeof(RemoteConfigAttribute), true).Any();
            var getValueMethod = GetType().GetMethod(nameof(GetValue)).MakeGenericMethod(prop.PropertyType);
            var value = isRemote ? getValueMethod.Invoke(this, new[] { prop.Name }) : prop.GetValue(_config.CurrentValue);
            if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && !typeof(string).IsAssignableFrom(prop.PropertyType))
            {
                var enumVal = (IEnumerable)value;
                value = string.Empty;
                foreach (var listVal in enumVal)
                {
                    value += listVal.ToString() + ", ";
                }
            }
            sb.AppendLine($"{prop.Name} (IsRemote: {isRemote}) => {value}");
        }
        return sb.ToString();
    }
}
