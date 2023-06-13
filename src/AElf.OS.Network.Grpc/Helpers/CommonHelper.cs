using System;
using AElf.Kernel;

namespace AElf.OS.Network.Grpc.Helpers;

public static class CommonHelper
{
    public static string GenerateRequestId()
    {
        var timeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var guid = Guid.NewGuid().ToString();
        return timeMs.ToString() + '_' + guid;
    }

    public static long GetRequestLatency(string requestId)
    {
        var sp = requestId.Split("_");
        if (sp.Length != 2) return -1;
        return long.TryParse(sp[0], out var start) ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start : -1;
    }

    public static bool GreaterThanSupportStreamMinVersion(this string version, string minVersion)
    {
        return Version.Parse(version).CompareTo(Version.Parse(minVersion)) >= 0;
    }
}