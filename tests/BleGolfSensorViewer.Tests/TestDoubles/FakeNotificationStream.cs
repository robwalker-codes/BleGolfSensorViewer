using System;
using System.Collections.Generic;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Tests.TestDoubles;

/// <summary>
/// Simple helper to trigger measurement events in tests.
/// </summary>
public sealed class FakeNotificationStream
{
    private readonly List<Measurement> _measurements = new();

    public event EventHandler<Measurement>? MeasurementPublished;

    public void AddMeasurement(Measurement measurement)
    {
        _measurements.Add(measurement);
    }

    public void PublishAll()
    {
        foreach (var measurement in _measurements)
        {
            MeasurementPublished?.Invoke(this, measurement);
        }
    }
}
