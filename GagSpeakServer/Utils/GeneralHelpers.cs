using System.Collections.Concurrent;

namespace GagspeakServer.Utils;

public class GeneralHelpers
{
    /// <summary>
    /// Attempts to get the message id of the selection board service.
    /// <para> General type function that acts as a helper for concurrent dictionaries </para>
    /// </summary> 
    public static Task<Tuple<bool, TKey>> TryGetKeyAsync<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary, TValue value)
    {
        // return a task that will run the function
        return Task.Run(() =>
        {
            // that will iterate through the dictionary and check if the value is equal to the value we are looking for
            foreach (var pair in dictionary)
            {
                // if it is, return a tuple with true and the key
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                {
                    // return a tuple with true and the key
                    return new Tuple<bool, TKey>(true, pair.Key);
                }
            }
            // if we don't find the value, return a tuple with false and the default key
            return new Tuple<bool, TKey>(false, default(TKey));
        });
    }
}