using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class BlockMiningService : IBlockMiningService
    {
        private readonly ITransactionListProvider _transactionListProvider;
        private readonly IContractTesterFactory _contractTesterFactory;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockTimeProvider _blockTimeProvider;

        private Round _currentRound;

        private Address _consensusContractAddress;

        private readonly List<AEDPoSContractImplContainer.AEDPoSContractImplStub> _contractStubs =
            new List<AEDPoSContractImplContainer.AEDPoSContractImplStub>();

        private bool _isSystemContractsDeployed;

        public BlockMiningService(ITransactionListProvider transactionListProvider,
            IContractTesterFactory contractTesterFactory, ISmartContractAddressService smartContractAddressService,
            IBlockTimeProvider blockTimeProvider)
        {
            _transactionListProvider = transactionListProvider;
            _contractTesterFactory = contractTesterFactory;
            _smartContractAddressService = smartContractAddressService;
            _blockTimeProvider = blockTimeProvider;
        }

        /// <summary>
        /// Only deploy provided contracts as system contract.
        /// Should initial each contract after if necessary.
        /// </summary>
        /// <param name="nameToCode"></param>
        /// <returns></returns>
        public async Task<Dictionary<Hash, Address>> DeploySystemContracts(Dictionary<Hash, byte[]> nameToCode)
        {
            var map = new Dictionary<Hash, Address>();
            var zeroContractStub =
                _contractTesterFactory.Create<BasicContractZeroContainer.BasicContractZeroStub>(
                    _smartContractAddressService.GetZeroSmartContractAddress(),
                    MissionedECKeyPairs.InitialKeyPairs.First());
            if (!nameToCode.Keys.Contains(ConsensusSmartContractAddressNameProvider.Name))
            {
                nameToCode.Add(ConsensusSmartContractAddressNameProvider.Name,
                    ContractsDeployer.GetContractCodes<ContractTestAEDPoSExtensionModule>().First().Value);
            }

            foreach (var (name, code) in nameToCode)
            {
                var address = (await zeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Name = name,
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(code),
                    })).Output;
                map.Add(name, address);
                if (name == ConsensusSmartContractAddressNameProvider.Name)
                {
                    _consensusContractAddress = address;
                }
            }

            _isSystemContractsDeployed = true;
            var currentBlockTime = TimestampHelper.GetUtcNow().ToDateTime();
            _blockTimeProvider.SetBlockTime(currentBlockTime);

            InitialContractStubs();
            InitialFirstRound(currentBlockTime);

            return map;
        }

        private void InitialContractStubs()
        {
            foreach (var initialKeyPair in MissionedECKeyPairs.InitialKeyPairs)
            {
                _contractStubs.Add(_contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                    _consensusContractAddress, initialKeyPair));
            }
        }

        private void InitialFirstRound(DateTime currentBlockTime)
        {
            var initialMinerList = new MinerList
            {
                Pubkeys = {MissionedECKeyPairs.InitialKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
            };
            _currentRound =
                initialMinerList.GenerateFirstRoundOfNewTerm(AEDPoSExtensionConstants.MiningInterval,
                    currentBlockTime);
            _contractStubs.First().FirstRound.SendAsync(_currentRound);
        }

        public async Task MineBlockAsync(List<Transaction> transactions)
        {
            if (!_isSystemContractsDeployed)
            {
                return;
            }

            await _transactionListProvider.AddTransactionListAsync(transactions);

            var currentBlockTime = _blockTimeProvider.GetBlockTime();

            var (contractStub, pubkey) = GetProperContractStub(currentBlockTime);
            var command = await contractStub.GetConsensusCommand.CallAsync(pubkey);
            var hint = AElfConsensusHint.Parser.ParseFrom(command.Hint);
            var triggerInformation = new AElfConsensusTriggerInformation
            {
                Behaviour = hint.Behaviour,
                // It doesn't matter for testing.
                RandomHash = Hash.FromMessage(pubkey),
                PreviousRandomHash = Hash.FromMessage(pubkey),
                Pubkey = pubkey.Value
            };
            var consensusTransaction = await contractStub.GenerateConsensusTransactions.CallAsync(new BytesValue
                {Value = triggerInformation.ToByteString()});
            await MineAsync(contractStub, consensusTransaction.Transactions.First());
            _currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
            _blockTimeProvider.SetBlockTime(currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval));

            await _transactionListProvider.ResetAsync();
        }

        private async Task MineAsync(AEDPoSContractImplContainer.AEDPoSContractImplStub contractStub,
            Transaction transaction)
        {
            switch (transaction.MethodName)
            {
                case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.UpdateValue):
                    await contractStub.UpdateValue.SendAsync(UpdateValueInput.Parser.ParseFrom(transaction.Params));
                    break;
                case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextRound):
                    await contractStub.NextRound.SendAsync(Round.Parser.ParseFrom(transaction.Params));
                    break;
                case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextTerm):
                    await contractStub.NextTerm.SendAsync(Round.Parser.ParseFrom(transaction.Params));
                    break;
            }
        }

        private (AEDPoSContractImplContainer.AEDPoSContractImplStub, BytesValue) GetProperContractStub(
            Timestamp currentBlockTime)
        {
            foreach (var minerInRound in _currentRound.RealTimeMinersInformation.Values)
            {
                if (minerInRound.ExpectedMiningTime == currentBlockTime)
                {
                    var pubkey = ByteArrayHelper.FromHexString(minerInRound.Pubkey);
                    return (_contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                        _consensusContractAddress,
                        SampleECKeyPairs.KeyPairs.First(p =>
                            p.PublicKey == pubkey)), new BytesValue {Value = ByteString.CopyFrom(pubkey)});
                }
            }

            throw new Exception("Proper contract stub not found.");
        }
    }

    internal static class MinerListExtension
    {
        internal static Round GenerateFirstRoundOfNewTerm(this MinerList miners, int miningInterval,
            DateTime currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
        {
            var sortedMiners =
                (from obj in miners.Pubkeys.Distinct()
                        .ToDictionary<ByteString, string, int>(miner => miner.ToHex(), miner => miner[0])
                    orderby obj.Value descending
                    select obj.Key).ToList();

            var round = new Round();

            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                // The first miner will be the extra block producer of first round of each term.
                if (i == 0)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.Pubkey = sortedMiners[i];
                minerInRound.Order = i + 1;
                minerInRound.ExpectedMiningTime =
                    currentBlockTime.AddMilliseconds((i * miningInterval) + miningInterval).ToTimestamp();
                // Should be careful during validation.
                minerInRound.PreviousInValue = Hash.Empty;

                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }

            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;

            return round;
        }
    }
}