using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AElf.Kernel.SmartContract.Grain;

public static class Constants
{
    public const int PreferedGatewayIndex = -1;
    public const int GatewayListRefreshPeriod = 10;
    public const int MinDotNetThreadPoolSize = 20480;
    public const int MinIOThreadPoolSize = 200;
    public const int DefaultConnectionLimit = 200;
    public const int MaxActiveThreads = 200;
    public const int ResponseTimeout = 30;
    
}