using Microsoft.Extensions.Logging;
using Prometheus;

namespace GagspeakShared.Metrics;

/// <summary> 
///     The metrics class for Gagspeak, used to track a number of interactions made across all users over the lifetime of GagSpeak.
/// </summary>
public class GagspeakMetrics
{
    public GagspeakMetrics(ILogger<GagspeakMetrics> logger, List<string> countersToServe, List<string> gaugesToServe)
    {
        logger.LogInformation("Initializing GagspeakMetrics");
        foreach (var counter in countersToServe)
        {
            logger.LogDebug($"Creating Metric for Counter {counter}");
            _counters.Add(counter, Prometheus.Metrics.CreateCounter(counter, counter));
        }

        foreach (var gauge in gaugesToServe)
        {
            logger.LogDebug($"Creating Metric for Counter {gauge}");
            _gauges.Add(gauge, Prometheus.Metrics.CreateGauge(gauge, gauge));
        }
    }

    private readonly Dictionary<string, Counter> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Gauge> _gauges = new(StringComparer.Ordinal);

    /// <summary> Increments a gauge with labels. </summary>
    public void IncGaugeWithLabels(string name, double value = 1.0, params string[] labels)
    {
        if (_gauges.TryGetValue(name, out Gauge gauge))
        {
            lock (gauge)
                gauge.WithLabels(labels).Inc(value);
        }
    }

    /// <summary> Decrements a gauge with labels. </summary>
    /// <param name="gaugeName">The Gauge to decrement</param>
    /// <param name="value">How much to dec it by</param>
    /// <param name="labels">The labels to bind to it the action.</param>
    public void DecGaugeWithLabels(string gaugeName, double value = 1.0, params string[] labels)
    {
        if (_gauges.TryGetValue(gaugeName, out Gauge gauge))
        {
            lock (gauge)
                gauge.WithLabels(labels).Dec(value);
        }
    }

    /// <summary> Sets a gauge to a specific value. </summary>
    public void SetGaugeTo(string gaugeName, double value)
    {
        if (_gauges.TryGetValue(gaugeName, out Gauge gauge))
        {
            lock (gauge)
                gauge.Set(value);
        }
    }

    /// <summary> Increments a gauge by an amount, or 1 by default. </summary>
    public void IncGauge(string gaugeName, double value = 1.0)
    {
        if (_gauges.TryGetValue(gaugeName, out Gauge gauge))
        {
            lock (gauge)
                gauge.Inc(value);
        }
    }

    /// <summary> Decrements a gauge by an amount, or 1 by default. </summary>
    public void DecGauge(string gaugeName, double value = 1.0)
    {
        if (_gauges.TryGetValue(gaugeName, out Gauge gauge))
        {
            lock (gauge)
                gauge.Dec(value);
        }
    }

    // Increments a counter by an amount, or 1 by default.
    public void IncCounter(string counterName, double value = 1.0)
    {
        if (_counters.TryGetValue(counterName, out Counter counter))
        {
            lock (counter)
                counter.Inc(value);
        }
    }
}