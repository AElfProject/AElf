using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Consensus.AEDPoS.LibConfirmation;
using AElf.Cryptography.Bls;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

internal class ConfirmBlockTransactionGenerator: ISystemTransactionGenerator, ISingletonDependency
{
    private readonly IAccountService _accountService;
    private readonly IBlockConfirmationService _blockConfirmationService;
    private readonly IConsensusReaderContextService _consensusReaderContextService;

    private readonly IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub>
        _contractReaderFactory;

    public ConfirmBlockTransactionGenerator(IAccountService accountService,
        IBlockConfirmationService blockConfirmationService,
        IConsensusReaderContextService consensusReaderContextService,
        IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub> contractReaderFactory)
    {
        _accountService = accountService;
        _consensusReaderContextService = consensusReaderContextService;
        _contractReaderFactory = contractReaderFactory;
        _blockConfirmationService = blockConfirmationService;
    }

    public ILogger<ConfirmBlockTransactionGenerator> Logger { get; set; }

    public async Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight, Hash preBlockHash)
    {
        var generatedTransactions = new List<Transaction>();

        if (preBlockHeight < AElfConstants.GenesisBlockHeight)
            return generatedTransactions;

        var chainContext = new ChainContext
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        };

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);

        var (blockIndex, dictionary) = await _blockConfirmationService.GetLatestConfirmedBlockAsync();
        var signatures = dictionary.Values.ToArray();
        var aggregatedSignature = BlsHelper.AggregateSignatures(signatures);
        var confirmBlockTx = _contractReaderFactory
            .Create(contractReaderContext).ConfirmBlock.GetTransaction(new ConfirmBlockInput
            {
                ConfirmedBlock = new ConfirmedBlock
                {
                    Hash = blockIndex.BlockHash,
                    Height = blockIndex.BlockHeight
                },
                AggregatedSignature = ByteString.CopyFrom(aggregatedSignature),
                SignedMiners =
                {
                    dictionary.Keys.Select(p =>
                        Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(p)))
                }
            });
        confirmBlockTx.RefBlockNumber = preBlockHeight;
        confirmBlockTx.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash);
        generatedTransactions.Add(confirmBlockTx);

        Logger.LogTrace("Tx ConfirmBlock generated.");
        return generatedTransactions;
    }
}