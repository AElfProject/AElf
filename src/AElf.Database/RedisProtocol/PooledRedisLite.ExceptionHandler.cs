using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.Database.RedisProtocol;

public partial class PooledRedisLite
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingNodes(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new RedisException("Got exception while get redis client.", ex)
        };
    }
}