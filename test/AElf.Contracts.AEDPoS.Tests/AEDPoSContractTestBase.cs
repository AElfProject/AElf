using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using BlockTimeProvider = AElf.ContractTestBase.ContractTestKit.BlockTimeProvider;

namespace AElf.Contracts.AEDPoS
{
    public class AEDPoSContractTestBase<T> : ContractTestBase<T> where T : AbpModule
    {
        public IEnumerable<Account> MinerAccounts => Accounts.Take(17);

        internal List<Miner> Miners => MinerAccounts.Select(a =>
            new Miner
            {
                Account = a,
                Stub = GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusAddress, a.KeyPair)
            }).ToList();

        internal BlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<BlockTimeProvider>();

        internal ITransactionResultQueryService TransactionResultQueryService =>
            Application.ServiceProvider.GetRequiredService<ITransactionResultQueryService>();

        internal IBlockchainService BlockchainService =>
            Application.ServiceProvider.GetRequiredService<IBlockchainService>();

        public AEDPoSContractTestBase()
        {
            BlockTimeProvider.SetBlockTime(new Timestamp());
        }

        internal async Task<Miner> GetNextMinerAsync()
        {
            var dict = new Dictionary<Miner, long>();
            foreach (var miner in Miners)
            {
                var consensusCommand =
                    await miner.Stub.GetConsensusCommand.CallAsync(new BytesValue
                        {Value = ByteString.CopyFrom(miner.Account.KeyPair.PublicKey)});
                var currentBlockTime = BlockTimeProvider.GetBlockTime();
                var distance = (consensusCommand.ArrangedMiningTime - currentBlockTime).ToTimeSpan()
                    .Milliseconds;
                miner.ConsensusCommand = consensusCommand;
                if (distance <= 4000)
                {
                    dict.Add(miner, distance);
                }
            }

            return dict.OrderBy(d => d.Value).First().Key;
        }

        internal async Task PackageConsensusTransactionAsync()
        {
            var triggerInformationProvider =
                Application.ServiceProvider.GetRequiredService<ITriggerInformationProvider>();
            var miner = await GetNextMinerAsync();
            var transaction = miner.Stub.GenerateConsensusTransactions.GetTransaction(
                triggerInformationProvider.GetTriggerInformationForConsensusTransactions(miner.ConsensusCommand
                    .ToBytesValue()));
            await MineAsync(new List<Transaction> {transaction}, miner.ConsensusCommand.ArrangedMiningTime);
            var txResult = await TransactionResultQueryService.GetTransactionResultAsync(transaction.GetHash());
            if (txResult == null || txResult.Status != TransactionResultStatus.Mined)
            {
                var chain = await BlockchainService.GetChainAsync();
                throw new Exception($"Height: {chain.BestChainHeight}");
            }
        }
    }

    public class Miner
    {
        public Account Account { get; set; }
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub Stub { get; set; }
        internal ConsensusCommand ConsensusCommand { get; set; }
    }
}