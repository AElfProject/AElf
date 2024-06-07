using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application;

public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
{
    private const int ExecutiveExpirationTime = 3600; // 1 Hour
    private const int ExecutiveClearLimit = 50;

    private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
    private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
    private readonly ITransactionContextFactory _transactionContextFactory;

    public SmartContractExecutiveService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
        ISmartContractRunnerContainer smartContractRunnerContainer,
        IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractExecutiveProvider smartContractExecutiveProvider,
        ITransactionContextFactory transactionContextFactory)
    {
        _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        _smartContractRunnerContainer = smartContractRunnerContainer;
        _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _smartContractExecutiveProvider = smartContractExecutiveProvider;
        _transactionContextFactory = transactionContextFactory;

        Logger = NullLogger<SmartContractExecutiveService>.Instance;
    }

    public ILogger<SmartContractExecutiveService> Logger { get; set; }

    public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
    {
        if (address == null) throw new ArgumentNullException(nameof(address));

        var pool = _smartContractExecutiveProvider.GetPool(address);
        var smartContractRegistration = await GetSmartContractRegistrationAsync(chainContext, address);

        if (!pool.TryTake(out var executive))
        {
            executive = await GetExecutiveAsync(smartContractRegistration);
        }
        else if (smartContractRegistration.CodeHash != executive.ContractHash)
        {
            _smartContractExecutiveProvider.TryRemove(address, out _);
            executive = await GetExecutiveAsync(smartContractRegistration);
        }

        return executive;
    }

    public virtual async Task PutExecutiveAsync(IChainContext chainContext, Address address, IExecutive executive)
    {
        if (_smartContractExecutiveProvider.TryGetValue(address, out var pool))
        {
            var smartContractRegistration =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext, address);
            if ((smartContractRegistration != null && smartContractRegistration.CodeHash == executive.ContractHash &&
                 pool.Count < ExecutiveClearLimit) ||
                chainContext.BlockHeight <= AElfConstants.GenesisBlockHeight)
            {
                executive.LastUsedTime = TimestampHelper.GetUtcNow();
                pool.Add(executive);
                return;
            }

            Logger.LogDebug($"Lost an executive (no registration {address})");
        }
        else
        {
            Logger.LogDebug($"Lost an executive (no pool {address})");
        }

        await Task.CompletedTask;
    }

    public void CleanExecutive(Address address)
    {
        _smartContractExecutiveProvider.TryRemove(address, out _);
    }

    public void CleanIdleExecutive()
    {
        var pools = _smartContractExecutiveProvider.GetExecutivePools();
        Logger.LogDebug($"Pools count {pools.Count}");
        var toBeRemoved = new List<Address>();
        foreach (var executivePool in pools)
        {
            var executiveBag = executivePool.Value;
            if (executiveBag.Count == 0)
                continue;

            if (executiveBag.Count > ExecutiveClearLimit || executiveBag.Min(o => o.LastUsedTime) <
                TimestampHelper.GetUtcNow() - TimestampHelper.DurationFromSeconds(ExecutiveExpirationTime))
            {
                if (executiveBag.TryTake(out _))
                    Logger.LogDebug($"Cleaned an idle executive for address {executivePool.Key}.");

                if (executiveBag.IsEmpty)
                    toBeRemoved.Add(executivePool.Key);
            }
        }

        // clean empty pools
        foreach (var address in toBeRemoved) CleanExecutive(address);
    }

    private async Task<IExecutive> GetExecutiveAsync(SmartContractRegistration reg)
    {
        // get runner
        var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

        // run smartContract executive info and return executive
        var executive = await runner.RunAsync(reg);

        var context = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(context);
        return executive;
    }

    private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
        IChainContext chainContext, Address address)
    {
        var smartContractRegistration =
            await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext, address);
        if (smartContractRegistration != null) return smartContractRegistration;

        smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(chainContext, address);

        return smartContractRegistration;
    }

    private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
        IChainContext chainContext, Address address)
    {
        IExecutive executiveZero = null;
        try
        {
            if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
            {
                var smartContractRegistration = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                if (chainContext.BlockHeight <= AElfConstants.GenesisBlockHeight) return smartContractRegistration;
                //if Height > GenesisBlockHeight, maybe there is a new zero contract,
                //the current smartContractRegistration is from code,
                //not from zero contract, so we need to load new zero contract from the old smartContractRegistration,
                //and replace it
                executiveZero = await GetExecutiveAsync(smartContractRegistration);
            }
            else
            {
                executiveZero =
                    await GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
            }

            return await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, address);
        }
        finally
        {
            if (executiveZero != null)
                await PutExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress,
                    executiveZero);
        }
    }

    private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
        IExecutive executiveZero, IChainContext chainContext, Address address)
    {
        var transaction = new Transaction
        {
            From = _defaultContractZeroCodeProvider.ContractZeroAddress,
            To = _defaultContractZeroCodeProvider.ContractZeroAddress,
            MethodName = "GetSmartContractRegistrationByAddress",
            Params = address.ToByteString()
        };

        var txContext = _transactionContextFactory.Create(transaction, chainContext);

        await executiveZero.ApplyAsync(txContext);
        var returnBytes = txContext.Trace?.ReturnValue;
        if (returnBytes != null && returnBytes != ByteString.Empty)
            return SmartContractRegistration.Parser.ParseFrom(returnBytes);

        throw new SmartContractFindRegistrationException(
            $"failed to find registration from zero contract {txContext.Trace.Error}");
    }
}