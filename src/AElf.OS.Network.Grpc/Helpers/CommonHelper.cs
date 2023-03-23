using System;

namespace AElf.OS.Network.Grpc.Helpers;

public class CommonHelper
{
    public static string GenerateRequestId()
    {
        var ticks = DateTime.Now.Ticks;
        var guid = Guid.NewGuid().ToString();
        return ticks.ToString() + '-' + guid;
    }
}