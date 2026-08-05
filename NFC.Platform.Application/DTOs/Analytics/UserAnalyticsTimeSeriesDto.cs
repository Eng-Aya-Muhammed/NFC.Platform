using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Analytics;

public class TimeSeriesDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Views { get; set; }
    public int ContactSaves { get; set; }
    public int LinkClicks { get; set; }
}

public class UserAnalyticsTimeSeriesDto
{
    public string Granularity { get; set; } = "monthly";
    public List<TimeSeriesDataPointDto> DataPoints { get; set; } = [];
}
