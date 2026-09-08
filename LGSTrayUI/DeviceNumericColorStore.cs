using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LGSTrayUI;

public sealed record DeviceNumericColors(string? Text = null, string? Background = null, bool? Bold = null);

public static class DeviceNumericColorStore
{
    public static Dictionary<string, DeviceNumericColors> Read(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, DeviceNumericColors>>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }

    public static DeviceNumericColors Get(string json, string deviceId) =>
        Read(json).GetValueOrDefault(deviceId) ?? new();

    public static DeviceNumericColors Resolve(string json, string deviceId, string text, string background, bool bold = false)
    {
        var device = Get(json, deviceId);
        return new(device.Text ?? text, device.Background ?? background, device.Bold ?? bold);
    }

    public static string Set(string json, string deviceId, DeviceNumericColors colors)
    {
        var entries = Read(json);
        if (colors.Text is null && colors.Background is null && colors.Bold is null) entries.Remove(deviceId);
        else entries[deviceId] = colors;
        return JsonSerializer.Serialize(entries);
    }
}
