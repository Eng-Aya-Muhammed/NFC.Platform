using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Analytics;

/// <summary>
/// A single data point in a time-series chart for analytics.
/// </summary>
public class TimeSeriesDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Views { get; set; }
    public int ContactSaves { get; set; }
    public int LinkClicks { get; set; }
}

/// <summary>
/// Time-series analytics data returned by GET /api/analytics/time-series.
/// Supports both daily and monthly granularity.
/// </summary>
public class UserAnalyticsTimeSeriesDto
{
    public string Granularity { get; set; } = "monthly";
    public List<TimeSeriesDataPointDto> DataPoints { get; set; } = [];
}
