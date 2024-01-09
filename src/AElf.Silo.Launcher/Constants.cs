using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AElf.Silo.Launcher;

public static class Constants
{
    public const int MinDotNetThreadPoolSize = 20480;
    public const int MinIOThreadPoolSize = 200;
    public const int DefaultConnectionLimit = 200;

    public const int DeactivationTimeout = 1;
    public const int CollectionAge = 2;
    public const int CollectionQuantum = 1;
    
    public const int DeathVoteExpirationTimeout = 1;
    public const int ProbeTimeout = 1;
    
    public const int ResponseTimeout = 10;
    public const int MaxActiveThreads = 200;
}