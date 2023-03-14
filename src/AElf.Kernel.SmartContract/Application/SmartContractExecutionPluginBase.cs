using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.Application;

public abstract class SmartContractExecutionPluginBase
{
    private readonly string _acsSymbol;

    protected SmartContractExecutionPluginBase(string acsSymbol)
    {
        _acsSymbol = acsSymbol;
    }

    protected bool HasApplicableAcs(IReadOnlyList<ServiceDescriptor> descriptors)
    {
        return descriptors.Any(service => service.File.GetIdentity() == _acsSymbol);
    }
}