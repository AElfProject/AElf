using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.ContractTestKit.AEDPoSExtension;

public class BlockMiningService : IBlockMiningService
{
    private readonly List<AuthorizationContractContainer.AuthorizationContractStub> _acs3Stubs = new();

    private readonly IChainTypeProvider _chainTypeProvider;

    private readonly List<AEDPoSContractImplContainer.AEDPoSContractImplStub> _contractStubs = new();

    private readonly IContractTesterFactory _contractTesterFactory;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITestDataProvider _testDataProvider;
    private readonly ITransactionResultService _transactionResultService;
    private readonly IBlockchainService _blockchainService;

    private Address _consensusContractAddress;

    private Round _currentRound;

    private bool _isSkipped;

    private bool _isSystemContractsDeployed;
    private Address _parliamentContractAddress;

    public BlockMiningService(IServiceProvider serviceProvider)
    {
        RegisterAssemblyResolveEvent();
        _contractTesterFactory = serviceProvider.GetRequiredService<IContractTesterFactory>();
        _smartContractAddressService = serviceProvider.GetRequiredService<ISmartContractAddressService>();
        _testDataProvider = serviceProvider.GetRequiredService<ITestDataProvider>();
        _transactionResultService = serviceProvider.GetRequiredService<ITransactionResultService>();
        _chainTypeProvider = serviceProvider.GetRequiredService<IChainTypeProvider>();
        _blockchainService = serviceProvider.GetRequiredService<IBlockchainService>();
    }

    /// <summary>
    ///     Only deploy provided contracts as system contract.
    ///     Should initial each contract after if necessary.
    /// </summary>
    /// <param name="nameToCode"></param>
    /// <param name="deployConsensusContract"></param>
    /// <returns></returns>
    public async Task<Dictionary<Hash, Address>> DeploySystemContractsAsync(Dictionary<Hash, byte[]> nameToCode,
        bool deployConsensusContract = true)
    {
        var map = new Dictionary<Hash, Address>();
        var zeroContractStub =
            _contractTesterFactory.Create<ACS0Container.ACS0Stub>(
                _smartContractAddressService.GetZeroSmartContractAddress(),
                MissionedECKeyPairs.InitialKeyPairs.First());
        if (!nameToCode.Keys.Contains(ConsensusSmartContractAddressNameProvider.Name) && deployConsensusContract)
            nameToCode.Add(ConsensusSmartContractAddressNameProvider.Name,
                ContractsDeployer.GetContractCodes<ContractTestAEDPoSExtensionModule>().First().Value);

        foreach (var (name, code) in nameToCode)
        {
            var address = (await zeroContractStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Name = name,
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code)
                })).Output;
            if (address == null)
            {
                //throw new Exception($"Failed to deploy contract {name}");
            }

            map.Add(name, address);
            if (name == ConsensusSmartContractAddressNameProvider.Name) _consensusContractAddress = address;

            if (name == ParliamentSmartContractAddressNameProvider.Name) _parliamentContractAddress = address;
        }

        _isSystemContractsDeployed = true;
        var currentBlockTime = TimestampHelper.GetUtcNow().ToDateTime();
        _testDataProvider.SetBlockTime(currentBlockTime.ToTimestamp()
            .AddMilliseconds(AEDPoSExtensionConstants.MiningInterval));

        InitialContractStubs();
        await InitialConsensus(currentBlockTime);

        return map;
    }

    public async Task MineBlockAsync(List<Transaction> transactions = null, bool withException = false)
    {
        if (!_isSystemContractsDeployed) return;

        if (transactions != null) await _testDataProvider.AddTransactionListAsync(transactions);

        var currentBlockTime = _testDataProvider.GetBlockTime();

        {
            {
                var currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
                if (currentRound.RoundNumber == 0)
                    throw new InitializationFailedException("Can't find current round information.");
            }
        }

        var maximumBlocksCount = (await _contractStubs.First().GetMaximumBlocksCount.CallAsync(new Empty())).Value;
        var (contractStub, pubkey) = GetProperContractStub(currentBlockTime, maximumBlocksCount);
        currentBlockTime = _testDataProvider.GetBlockTime();

        {
            var currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
            if (currentRound.RoundNumber == 0)
                throw new InitializationFailedException("Can't find current round information.");
        }

        var randomNumber = await GenerateRandomProofAsync();
        var triggerInformation = await GetConsensusTriggerInfoAsync(contractStub, pubkey, ByteString.CopyFrom(randomNumber));
        var consensusTransaction = await contractStub.GenerateConsensusTransactions.CallAsync(new BytesValue
        {
            Value = triggerInformation.ToByteString()
        });
        await MineAsync(contractStub, consensusTransaction.Transactions.First(), withException);
        _currentRound = await _contractStubs.First().GetCurrentRoundInformation.CallAsync(new Empty());
        Debug.WriteLine($"Update current round information.{_currentRound}");
        if (!_isSkipped)
            if (_currentRound.RealTimeMinersInformation.Any(i => i.Value.MissedTimeSlots != 0))
            {
                var previousRound = await _contractStubs.First().GetPreviousRoundInformation.CallAsync(new Empty());
                throw new BlockMiningException(
                    $"Someone missed time slot.\n{_currentRound}\n{previousRound}\nCurrent block time: {currentBlockTime}");
            }

        _testDataProvider.SetBlockTime(
            consensusTransaction.Transactions.First().MethodName ==
            nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextTerm)
                ? currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval)
                : currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.ActualMiningInterval));

        await _testDataProvider.ResetAsync();

        _isSkipped = false;
    }

    private async Task<byte[]> GenerateRandomProofAsync()
    {
        var blockHeight = (await _blockchainService.GetChainAsync()).BestChainHeight;
        var previousRandomHash =
            blockHeight <= 1
                ? Hash.Empty
                : await _contractStubs.First().GetRandomHash.CallAsync(new Int64Value
                    { Value = blockHeight });
        return CryptoHelper.ECVrfProve((ECKeyPair)_testDataProvider.GetKeyPair(), previousRandomHash.ToByteArray());
    }

    public async Task<long> MineBlockToNextRoundAsync()
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

        return currentRoundNumber;
    }

    public async Task<long> MineBlockToNextTermAsync()
    {
        var consensusStub = _contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
            _consensusContractAddress, MissionedECKeyPairs.InitialKeyPairs.First());
        var startTermNumber = (await consensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
        var currentTermNumber = startTermNumber;
        while (currentTermNumber == startTermNumber)
        {
            await MineBlockAsync();
            currentTermNumber = (await consensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
        }

        return currentTermNumber;
    }

    public async Task MineBlockAsync(long targetHeight)
    {
        var startHeight = await _testDataProvider.GetCurrentBlockHeight();
        if (targetHeight <= startHeight) return;

        var currentHeight = startHeight;
        while (currentHeight <= targetHeight)
        {
            await MineBlockAsync();
            currentHeight = await _testDataProvider.GetCurrentBlockHeight();
        }
    }

    /// <summary>
    ///     Skip a certain time for missing some blocks deliberately.
    /// </summary>
    /// <param name="seconds"></param>
    public void SkipTime(int seconds)
    {
        var timestamp = _testDataProvider.GetBlockTime();
        _testDataProvider.SetBlockTime(timestamp.AddSeconds(seconds));
        _isSkipped = true;
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

    private void InitialContractStubs()
    {
        foreach (var initialKeyPair in MissionedECKeyPairs.InitialKeyPairs.Concat(
                     MissionedECKeyPairs.ValidationDataCenterKeyPairs.Take(18)))
            _contractStubs.Add(_contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                _consensusContractAddress, initialKeyPair));
    }

    private async Task InitialConsensus(DateTime currentBlockTime)
    {
        // InitialAElfConsensusContract
        {
            var executionResult = await _contractStubs.First().InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    MinerIncreaseInterval = AEDPoSExtensionConstants.MinerIncreaseInterval,
                    PeriodSeconds = AEDPoSExtensionConstants.PeriodSeconds,
                    IsSideChain = _chainTypeProvider.IsSideChain
                });
            if (executionResult.TransactionResult.Status != TransactionResultStatus.Mined)
                throw new InitializationFailedException("Failed to execute InitialAElfConsensusContract.",
                    executionResult.TransactionResult.Error);
        }

        var initialMinerList = new MinerList
        {
            Pubkeys = { MissionedECKeyPairs.InitialKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) }
        };
        _currentRound =
            initialMinerList.GenerateFirstRoundOfNewTerm(AEDPoSExtensionConstants.MiningInterval,
                currentBlockTime);
        _testDataProvider.SetBlockTime(currentBlockTime.ToTimestamp());

        // FirstRound
        {
            var executionResult = await _contractStubs.First().FirstRound.SendAsync(_currentRound);
            if (executionResult.TransactionResult.Status != TransactionResultStatus.Mined)
                throw new InitializationFailedException("Failed to execute FirstRound.",
                    executionResult.TransactionResult.Error);
        }
        _testDataProvider.SetBlockTime(currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval)
            .ToTimestamp());
    }

    private async Task MineAsync(AEDPoSContractImplContainer.AEDPoSContractImplStub contractStub,
        Transaction transaction, bool withException = false)
    {
        switch (transaction.MethodName)
        {
            case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.UpdateTinyBlockInformation):
                if (withException)
                    await contractStub.UpdateTinyBlockInformation.SendWithExceptionAsync(
                        TinyBlockInput.Parser.ParseFrom(transaction.Params));
                else
                    await contractStub.UpdateTinyBlockInformation.SendAsync(
                        TinyBlockInput.Parser.ParseFrom(transaction.Params));

                break;
            case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.UpdateValue):
                if (withException)
                    await contractStub.UpdateValue.SendWithExceptionAsync(
                        UpdateValueInput.Parser.ParseFrom(transaction.Params));
                else
                    await contractStub.UpdateValue.SendAsync(UpdateValueInput.Parser.ParseFrom(transaction.Params));

                break;
            case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextRound):
                if (withException)
                    await contractStub.NextRound.SendWithExceptionAsync(NextRoundInput.Parser.ParseFrom(transaction.Params));
                else
                    await contractStub.NextRound.SendAsync(NextRoundInput.Parser.ParseFrom(transaction.Params));

                break;
            case nameof(AEDPoSContractImplContainer.AEDPoSContractImplStub.NextTerm):
                if (withException)
                    await contractStub.NextTerm.SendWithExceptionAsync(NextTermInput.Parser.ParseFrom(transaction.Params));
                else
                    await contractStub.NextTerm.SendAsync(NextTermInput.Parser.ParseFrom(transaction.Params));

                break;
        }
    }

    private (AEDPoSContractImplContainer.AEDPoSContractImplStub, BytesValue) GetProperContractStub(
        Timestamp currentBlockTime, int maximumBlocksCount)
    {
        try
        {
            if (_currentRound.RoundNumber == 0) throw new BlockMiningException("Invalid round information.");

            var roundStartTime = _currentRound.RealTimeMinersInformation.Single(i => i.Value.Order == 1).Value
                .ExpectedMiningTime;

            var roundEndTime = _currentRound.RealTimeMinersInformation
                .Single(i => i.Value.Order == _currentRound.RealTimeMinersInformation.Count).Value
                .ExpectedMiningTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval);
            if (currentBlockTime > roundEndTime) throw new BlockMiningException("Failed to find proper contract stub.");

            var ebp = _currentRound.RealTimeMinersInformation.Values.FirstOrDefault(i =>
                i.Pubkey == _currentRound.ExtraBlockProducerOfPreviousRound);
            if (ebp != null && _currentRound.RealTimeMinersInformation.Values.All(i => i.OutValue == null) &&
                currentBlockTime < roundStartTime && ebp.ActualMiningTimes.Count + 1 <= maximumBlocksCount)
            {
                Debug.WriteLine("Tiny block before new round.");
                return ProperContractStub(ebp);
            }

            foreach (var minerInRound in _currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                         .ToList())
            {
                if (minerInRound.ExpectedMiningTime <= currentBlockTime && currentBlockTime <
                    minerInRound.ExpectedMiningTime.AddMilliseconds(AEDPoSExtensionConstants.MiningInterval) &&
                    (minerInRound.ActualMiningTimes.Count + 1 <= maximumBlocksCount ||
                     (minerInRound.Pubkey == _currentRound.ExtraBlockProducerOfPreviousRound &&
                      minerInRound.ActualMiningTimes.Count + 2 <= maximumBlocksCount * 2)))
                {
                    Debug.WriteLine("Normal block or tiny block.");
                    return ProperContractStub(minerInRound);
                }

                var minersCount = _currentRound.RealTimeMinersInformation.Count;
                if (minerInRound.IsExtraBlockProducer &&
                    _currentRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null) == minersCount)
                {
                    Debug.WriteLine("End of current round.");
                    return ProperContractStub(minerInRound);
                }
            }
        }
        catch (Exception e)
        {
            throw new BlockMiningException("Failed to find proper contract stub.", e);
        }

        _testDataProvider.SetBlockTime(AEDPoSExtensionConstants.ActualMiningInterval);
        Debug.WriteLine("Move forward time.");
        return GetProperContractStub(
            currentBlockTime.AddMilliseconds(AEDPoSExtensionConstants.ActualMiningInterval), maximumBlocksCount);
    }

    private (AEDPoSContractImplContainer.AEDPoSContractImplStub, BytesValue) ProperContractStub(
        MinerInRound minerInRound)
    {
        var pubkey = ByteArrayHelper.HexStringToByteArray(minerInRound.Pubkey);
        var keyPair = SampleAccount.Accounts.First(a => a.KeyPair.PublicKey.BytesEqual(pubkey)).KeyPair;
        _testDataProvider.SetKeyPair(keyPair);
        Debug.WriteLine($"Chosen miner: {keyPair.PublicKey.ToHex()}");
        return (_contractTesterFactory.Create<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
            _consensusContractAddress, keyPair), new BytesValue { Value = ByteString.CopyFrom(pubkey) });
    }

    private async Task<AElfConsensusTriggerInformation> GetConsensusTriggerInfoAsync(
        AEDPoSContractImplContainer.AEDPoSContractImplStub contractStub, BytesValue pubkey, ByteString randomNumber)
    {
        var command = await contractStub.GetConsensusCommand.CallAsync(pubkey);
        var hint = AElfConsensusHint.Parser.ParseFrom(command.Hint);
        var triggerInformation = new AElfConsensusTriggerInformation
        {
            Behaviour = hint.Behaviour,
            // It doesn't matter for testing.
            InValue = HashHelper.ComputeFrom($"InValueOf{pubkey}"),
            PreviousInValue = HashHelper.ComputeFrom($"InValueOf{pubkey}"),
            Pubkey = pubkey.Value,
            RandomNumber = randomNumber
        };

        var consensusExtraData = await contractStub.GetConsensusExtraData.CallAsync(new BytesValue
        {
            Value = triggerInformation.ToByteString()
        });
        var consensusHeaderInformation = new AElfConsensusHeaderInformation();
        consensusHeaderInformation.MergeFrom(consensusExtraData.Value);
        Debug.WriteLine($"Current header information: {consensusHeaderInformation}");

        // Validate consensus extra data.
        {
            var validationResult =
                await _contractStubs.First().ValidateConsensusBeforeExecution.CallAsync(consensusExtraData);
            if (!validationResult.Success)
                throw new Exception($"Consensus extra data validation failed: {validationResult.Message}");
        }

        return triggerInformation;
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
            if (i == 0) minerInRound.IsExtraBlockProducer = true;

            minerInRound.Pubkey = sortedMiners[i];
            minerInRound.Order = i + 1;
            minerInRound.ExpectedMiningTime =
                currentBlockTime.AddMilliseconds(i * miningInterval + miningInterval).ToTimestamp();
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