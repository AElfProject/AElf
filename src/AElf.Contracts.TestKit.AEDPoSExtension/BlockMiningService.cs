using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class BlockMiningService : IBlockMiningService
    {
        private readonly ITestDataProvider _testDataProvider;
        private readonly IContractTesterFactory _contractTesterFactory;
        private readonly ISmartContractAddressService _smartContractAddressService;

        private Round _currentRound;

        private Address _consensusContractAddress;

        private readonly List<AEDPoSContractImplContainer.AEDPoSContractImplStub> _contractStubs =
            new List<AEDPoSContractImplContainer.AEDPoSContractImplStub>();

        private bool _isSystemContractsDeployed;
        
        private bool _isSkipped;

        public BlockMiningService(IServiceProvider serviceProvider)
        {
            RegisterAssemblyResolveEvent();
            _contractTesterFactory = serviceProvider.GetRequiredService<IContractTesterFactory>();
            _smartContractAddressService = serviceProvider.GetRequiredService<ISmartContractAddressService>();
            _testDataProvider = serviceProvider.GetRequiredService<ITestDataProvider>();
        }

        private static void RegisterAssemblyResolveEvent()
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            var assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        /// <summary>
        /// Only deploy provided contracts as system contract.
        /// Should initial each contract after if necessary.
        /// </summary>
        /// <param name="nameToCode"></param>
        /// <returns></returns>
        public async Task<Dictionary<Hash, Address>> DeploySystemContractsAsync(Dictionary<Hash, byte[]> nameToCode)
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
                if (address == null)
                {
                    //throw new Exception($"Failed to deploy contract {name}");
                }
                map.Add(name, address);
                if (name == ConsensusSmartContractAddressNameProvider.Name)
                {
                    _consensusContractAddress = address;
                }
            }

            _isSystemContractsDeployed = true;
            var currentBlockTime = TimestampHelper.GetUtcNow().ToDateTime();
            _testDataProvider.SetBlockTime(currentBlockTime.ToTimestamp()
                .AddMilliseconds(AEDPoSExtensionConstants.MiningInterval));

            InitialContractStubs();
            await InitialConsensus(currentBlockTime);

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

        private async Task InitialConsensus(DateTime currentBlockTime)
        {
            // InitialAElfConsensusContract
            {
                var executionResult = await _contractStubs.First().InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        MinerIncreaseInterval = AEDPoSExtensionConstants.MinerIncreaseInterval,
                        TimeEachTerm = AEDPoSExtensionConstants.TimeEachTerm
                    });
                if (executionResult.TransactionResult.Status != TransactionResultStatus.Mined)
                {
                    throw new InitializationFailedException("Failed to execute InitialAElfConsensusContract.",
                        executionResult.TransactionResult.Error);
                }
            }

            var initialMinerList = new MinerList
            {
                Pubkeys = {MissionedECKeyPairs.InitialKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
            };
            _currentRound =
                initialMinerList.GenerateFirstRoundOfNewTerm(AEDPoSExtensionConstants.MiningInterval,
                    currentBlockTime);
            _testDataProvider.SetBlockTime(currentBlockTime.ToTimestamp());

            // FirstRound
            {
                var executionResult = await _contractStubs.First().FirstRound.SendAsync(_currentRound);
                if (executionResult.TransactionResult.Status != TransactionResultStatus.Mined)
                {
                    throw new InitializationFailedException("Failed to execute FirstRound.",
                        executionResult.TransactionResult.Error);
                }
            }
            _testDataProvider.SetBlockTime(currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval)
                .ToTimestamp());
        }

        public async Task MineBlockAsync(List<Transaction> transactions = null)
        {
            if (!_isSystemContractsDeployed)
            {
                return;
            }

            if (transactions != null)
            {
                await _testDataProvider.AddTransactionListAsync(transactions);
            }

            var currentBlockTime = _testDataProvider.GetBlockTime();

            {
                {
                    var currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
                    if (currentRound.RoundNumber == 0)
                    {
                        throw new InitializationFailedException("Can't find current round information.");
                    }
                }
            }

            var (contractStub, pubkey) =
                GetProperContractStub(currentBlockTime);
            currentBlockTime = _testDataProvider.GetBlockTime();

            {
                var currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
                if (currentRound.RoundNumber == 0)
                {
                    throw new InitializationFailedException("Can't find current round information.");
                }
            }

            var command = await contractStub.GetConsensusCommand.CallAsync(pubkey);
            var hint = AElfConsensusHint.Parser.ParseFrom(command.Hint);
            var triggerInformation = new AElfConsensusTriggerInformation
            {
                Behaviour = hint.Behaviour,
                // It doesn't matter for testing.
                InValue = Hash.FromString($"InValueOf{pubkey}"),
                PreviousInValue = Hash.FromString($"InValueOf{pubkey}"),
                Pubkey = pubkey.Value
            };

            var consensusExtraData = await contractStub.GetConsensusExtraData.CallAsync(new BytesValue
            {
                Value = triggerInformation.ToByteString()
            });
            // Validate consensus extra data.
            if (consensusExtraData != null)
            {
                var validationResult =
                    await _contractStubs.First().ValidateConsensusBeforeExecution.CallAsync(consensusExtraData);
                if (!validationResult.Success)
                {
                    throw new Exception($"Consensus extra data validation failed: {validationResult.Message}");
                }
            }

            var consensusTransaction = await contractStub.GenerateConsensusTransactions.CallAsync(new BytesValue
                {Value = triggerInformation.ToByteString()});
            await MineAsync(contractStub, consensusTransaction.Transactions.First());
            _currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
            if (!_isSkipped)
            {
                if (_currentRound.RealTimeMinersInformation.Any(i => i.Value.MissedTimeSlots != 0))
                {
                    var previousRound = await _contractStubs.First().GetPreviousRoundInformation.CallAsync(new Empty());
                    //throw new BlockMiningException(
                        //$"Someone missed time slot.\n{_currentRound}\n{previousRound}\nCurrent block time: {currentBlockTime}");
                }
            }

            _testDataProvider.SetBlockTime(
                consensusTransaction.Transactions.First().MethodName ==
                nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextTerm)
                    ? currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval)
                    : currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.ActualMiningInterval));

            await _testDataProvider.ResetAsync();

            _isSkipped = false;
        }

        public async Task MineBlockToNextRoundAsync()
        {
            var consensusStub = _contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                _consensusContractAddress, MissionedECKeyPairs.InitialKeyPairs.First());
            var startRoundNumber = (await consensusStub.GetCurrentRoundNumber.CallAsync(new Empty())).Value;
            var currentRoundNumber = startRoundNumber;
            while (currentRoundNumber == startRoundNumber)
            {
                await MineBlockAsync();
                currentRoundNumber = (await consensusStub.GetCurrentRoundNumber.CallAsync(new Empty())).Value;
            }
        }

        public async Task MineBlockAsync(long targetHeight)
        {
            var startHeight = await _testDataProvider.GetCurrentBlockHeight();
            if (targetHeight <= startHeight)
            {
                return;
            }
            var currentHeight = startHeight;
            while (currentHeight <= targetHeight)
            {
                await MineBlockAsync();
                currentHeight = await _testDataProvider.GetCurrentBlockHeight();
            }
        }

        /// <summary>
        /// Skip a certain time for missing some blocks deliberately.
        /// </summary>
        /// <param name="seconds"></param>
        public void SkipTime(int seconds)
        {
            var timestamp = _testDataProvider.GetBlockTime();
            _testDataProvider.SetBlockTime(timestamp.AddSeconds(seconds));
            _isSkipped = true;
        }

        private async Task MineAsync(AEDPoSContractImplContainer.AEDPoSContractImplStub contractStub,
            Transaction transaction)
        {
            switch (transaction.MethodName)
            {
                case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.UpdateTinyBlockInformation):
                    await contractStub.UpdateTinyBlockInformation.SendAsync(
                        TinyBlockInput.Parser.ParseFrom(transaction.Params));
                    break;
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
            try
            {
                if (_currentRound.RoundNumber == 0)
                {
                    throw new BlockMiningException("Invalid round information.");
                }

                var roundStartTime = _currentRound.RealTimeMinersInformation.Single(i => i.Value.Order == 1).Value
                    .ExpectedMiningTime;
                if (_currentRound.RealTimeMinersInformation.Values.All(i => i.OutValue == null) &&
                    currentBlockTime < roundStartTime)
                {
                    return ProperContractStub(_currentRound.RealTimeMinersInformation.Values.Single(i =>
                        i.Pubkey == _currentRound.ExtraBlockProducerOfPreviousRound));
                }

                foreach (var minerInRound in _currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                    .ToList())
                {
                    if (minerInRound.ExpectedMiningTime <= currentBlockTime && currentBlockTime <
                        minerInRound.ExpectedMiningTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval))
                    {
                        return ProperContractStub(minerInRound);
                    }

                    var minersCount = _currentRound.RealTimeMinersInformation.Count;
                    if (minerInRound.IsExtraBlockProducer &&
                        _currentRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null) == minersCount)
                    {
                        return ProperContractStub(minerInRound);
                    }
                }
            }
            catch (Exception e)
            {
                throw new BlockMiningException("Failed to find proper contract stub.", e);
            }

            //throw new BlockMiningException($"Proper contract stub not found.\n{_currentRound}\n{currentBlockTime}");

            _testDataProvider.SetBlockTime(AEDPoSExtensionConstants.ActualMiningInterval);
            return GetProperContractStub(currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.ActualMiningInterval));
        }

        private (AEDPoSContractImplContainer.AEDPoSContractImplStub, BytesValue) ProperContractStub(MinerInRound minerInRound)
        {
            var pubkey = ByteArrayHelper.HexStringToByteArray(minerInRound.Pubkey);
            var keyPair = SampleECKeyPairs.KeyPairs.First(p => p.PublicKey.BytesEqual(pubkey));
            _testDataProvider.SetKeyPair(keyPair);
            return (_contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                _consensusContractAddress, keyPair), new BytesValue {Value = ByteString.CopyFrom(pubkey)});
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
            round.IsMinerListJustChanged = true;

            return round;
        }
    }
}