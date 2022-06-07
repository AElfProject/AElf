using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.Node.Events;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Node.Application;

public class BlockchainNodeContextStartDto
{
    public int ChainId { get; set; }

    public Transaction[] Transactions { get; set; }

    public Type ZeroSmartContractType { get; set; }
}

public interface IBlockchainNodeContextService
{
    Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto);

    Task StopAsync(BlockchainNodeContext blockchainNodeContext);

    Task FinishInitialSyncAsync();
}

//Maybe we should call it CSharpBlockchainNodeContextService, or we should spilt the logic depended on CSharp
public class BlockchainNodeContextService : IBlockchainNodeContextService
{
    private readonly IBlockchainService _blockchainService;
    private readonly IChainCreationService _chainCreationService;
    private readonly IConsensusService _consensusService;
    private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

    public BlockchainNodeContextService(
        IBlockchainService blockchainService, IChainCreationService chainCreationService,
        IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider, IConsensusService consensusService)
    {
        _blockchainService = blockchainService;
        _chainCreationService = chainCreationService;
        _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        _consensusService = consensusService;

        EventBus = NullLocalEventBus.Instance;
    }

    public ILocalEventBus EventBus { get; set; }

    public async Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto)
    {
        _defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(dto.ZeroSmartContractType);

        var chain = await _blockchainService.GetChainAsync();
        chain = chain == null
            ? await _chainCreationService.CreateNewChainAsync(dto.Transactions)
            : await _blockchainService.ResetChainToLibAsync(chain);

        await _consensusService.TriggerConsensusAsync(new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        });

        return new BlockchainNodeContext
        {
            ChainId = chain.Id
        };
    }

    public async Task FinishInitialSyncAsync()
    {
        await EventBus.PublishAsync(new InitialSyncFinishedEvent());
    }

    public Task StopAsync(BlockchainNodeContext blockchainNodeContext)
    {
        return Task.CompletedTask;
    }
}