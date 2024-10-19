using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.Database.RedisProtocol;

public static partial class RedisExtensions
{
    internal static async Task<FlowBehavior> HandleSocketException(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}