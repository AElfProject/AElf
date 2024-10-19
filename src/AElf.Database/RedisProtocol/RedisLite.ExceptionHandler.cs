using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.Database.RedisProtocol;

public partial class RedisLite
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileConnecting(SocketException ex)
    {
        socket?.Close();
        socket = null;

        HadExceptions = true;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new Exception("could not connect to redis Instance at " + Host + ":" + Port, ex)
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileSendingCommand(SocketException ex)
    {
        _cmdBuffer.Clear();
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = HandleSocketException(ex)
        };
    }
}