using System;
using AElf.ExceptionHandler;

namespace AElf.Kernel.SmartContract;

public partial class HostSmartContractBridgeContext
{
    protected virtual FlowBehavior HandleExceptionWhileVrfVerifingEC(Exception ex, byte[] pubKey, byte[] alpha, byte[] pi, out byte[] beta)
    {
        beta = null;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}