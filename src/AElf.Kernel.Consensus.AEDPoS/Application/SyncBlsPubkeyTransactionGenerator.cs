using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
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

internal class SyncBlsPubkeyTransactionGenerator : ISystemTransactionGenerator, ISingletonDependency
{
    private readonly IAccountService _accountService;
    private readonly IConsensusReaderContextService _consensusReaderContextService;

    private readonly IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub>
        _contractReaderFactory;

    public SyncBlsPubkeyTransactionGenerator(IAccountService accountService,
        IConsensusReaderContextService consensusReaderContextService,
        IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub> contractReaderFactory)
    {
        _accountService = accountService;
        _consensusReaderContextService = consensusReaderContextService;
        _contractReaderFactory = contractReaderFactory;
    }

    public ILogger<SyncBlsPubkeyTransactionGenerator> Logger { get; set; }

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
        var maybeBlsPubkey =
            await _contractReaderFactory
                .Create(contractReaderContext).GetBlsPubkey
                .CallAsync(from);
        if (!maybeBlsPubkey.ToByteArray().IsNullOrEmpty())
        {
            return generatedTransactions;
        }

        var blsPubkey = await _accountService.GetBlsPubkeyAsync();
        var syncBlsPubkeyTx = _contractReaderFactory
            .Create(contractReaderContext).SyncBlsPubkey.GetTransaction(new BytesValue
            {
                Value = ByteString.CopyFrom(blsPubkey)
            });
        syncBlsPubkeyTx.RefBlockNumber = preBlockHeight;
        syncBlsPubkeyTx.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash);
        generatedTransactions.Add(syncBlsPubkeyTx);

        Logger.LogTrace("Tx SyncBlsPubkey generated.");
        return generatedTransactions;
    }
}