using System.Diagnostics;
using AElf.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

[DependsOn(typeof(ContractTestModule),
    typeof(ExecutionPluginForResourceFeeModule))]
public class SideChainRentFeeModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.RemoveAll<IPreExecutionPlugin>();
        context.Services.Replace(ServiceDescriptor
            .Singleton<IBlockTimeProvider, DelayBlockTimeProvider>());
    }
}

public class DelayBlockTimeProvider : IBlockTimeProvider
{
    private const int DelaySeconds = 10;
    private Timestamp _blockTime;

    public Timestamp GetBlockTime()
    {
        return _blockTime == null ? TimestampHelper.GetUtcNow() : _blockTime.AddSeconds(DelaySeconds);
    }

    public void SetBlockTime(Timestamp blockTime)
    {
        Debug.WriteLine($"Update block time: {blockTime}");
        _blockTime = blockTime;
    }

    public void SetBlockTime(int offsetMilliseconds)
    {
        SetBlockTime(_blockTime.AddMilliseconds(offsetMilliseconds));
    }
}