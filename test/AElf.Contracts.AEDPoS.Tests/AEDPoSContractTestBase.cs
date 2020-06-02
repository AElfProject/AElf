using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestKit;
using AElf.ContractTestBase;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.AEDPoS
{
    public class AEDPoSContractTestBase : MainChainContractTestBase<AEDPoSContractTestAElfModule>
    {
        public IEnumerable<Account> MinerAccounts => SampleAccount.Accounts.Take(17);

        internal List<Miner> Miners => MinerAccounts.Select(a =>
            new Miner
            {
                Account = a,
                Stub = GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusAddress, a.KeyPair)
            }).ToList();

        internal async Task<Miner> GetNextMinerAsync()
        {
            foreach (var miner in Miners)
            {
                var consensusCommand =
                    await miner.Stub.GetConsensusCommand.CallAsync(new BytesValue
                        {Value = ByteString.CopyFrom(miner.Account.KeyPair.PublicKey)});
                var distance = (consensusCommand.ArrangedMiningTime - TimestampHelper.GetUtcNow()).ToTimeSpan()
                    .Milliseconds;
                if (distance == 500)
                {
                    return miner;
                }
            }

            return null;
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
        }
    }

    public class Miner
    {
        public Account Account { get; set; }
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub Stub { get; set; }
        internal ConsensusCommand ConsensusCommand { get; set; }
    }
}