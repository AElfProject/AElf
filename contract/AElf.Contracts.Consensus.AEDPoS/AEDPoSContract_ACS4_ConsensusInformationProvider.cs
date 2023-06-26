using System.Linq;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public partial class AEDPoSContract
{
    /// <summary>
    ///     In this method, `Context.CurrentBlockTime` is the time one miner start request his next consensus command.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override ConsensusCommand GetConsensusCommand(BytesValue input)
    {
        _processingBlockMinerPubkey = input.Value.ToHex();

        if (Context.CurrentHeight < 2) return ConsensusCommandProvider.InvalidConsensusCommand;

        if (!TryToGetCurrentRoundInformation(out var currentRound))
            return ConsensusCommandProvider.InvalidConsensusCommand;

        if (!currentRound.IsInMinerList(_processingBlockMinerPubkey))
            return ConsensusCommandProvider.InvalidConsensusCommand;

        if (currentRound.RealTimeMinersInformation.Count != 1 &&
            currentRound.RoundNumber > 2 &&
            State.LatestPubkeyToTinyBlocksCount.Value != null &&
            State.LatestPubkeyToTinyBlocksCount.Value.Pubkey == _processingBlockMinerPubkey &&
            State.LatestPubkeyToTinyBlocksCount.Value.BlocksCount < 0)
            return GetConsensusCommand(AElfConsensusBehaviour.NextRound, currentRound, _processingBlockMinerPubkey,
                Context.CurrentBlockTime);

        var blockchainStartTimestamp = GetBlockchainStartTimestamp();

        var behaviour = IsMainChain
            ? new MainChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey,
                    GetMaximumBlocksCount(),
                    Context.CurrentBlockTime, blockchainStartTimestamp, State.PeriodSeconds.Value)
                .GetConsensusBehaviour()
            : new SideChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey,
                GetMaximumBlocksCount(),
                Context.CurrentBlockTime).GetConsensusBehaviour();

        Context.LogDebug(() =>
            $"{currentRound.ToString(_processingBlockMinerPubkey)}\nArranged behaviour: {behaviour.ToString()}");

        return behaviour == AElfConsensusBehaviour.Nothing
            ? ConsensusCommandProvider.InvalidConsensusCommand
            : GetConsensusCommand(behaviour, currentRound, _processingBlockMinerPubkey, Context.CurrentBlockTime);
    }

    public override BytesValue GetConsensusExtraData(BytesValue input)
    {
        return GetConsensusBlockExtraData(input);
    }

    public override TransactionList GenerateConsensusTransactions(BytesValue input)
    {
        var triggerInformation = new AElfConsensusTriggerInformation();
        triggerInformation.MergeFrom(input.Value);
        // Some basic checks.
        Assert(triggerInformation.Pubkey.Any(),
            "Data to request consensus information should contain pubkey.");

        var pubkey = triggerInformation.Pubkey;
        var randomNumber = triggerInformation.RandomNumber;
        var consensusInformation = new AElfConsensusHeaderInformation();
        consensusInformation.MergeFrom(GetConsensusBlockExtraData(input, true).Value);
        var transactionList = GenerateTransactionListByExtraData(consensusInformation, pubkey, randomNumber);
        return transactionList;
    }

    public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
    {
        var extraData = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value.ToByteArray());
        return ValidateBeforeExecution(extraData);
    }

    public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
    {
        var headerInformation = new AElfConsensusHeaderInformation();
        headerInformation.MergeFrom(input.Value);
        if (TryToGetCurrentRoundInformation(out var currentRound))
        {
            if (headerInformation.Behaviour == AElfConsensusBehaviour.UpdateValue)
                headerInformation.Round =
                    currentRound.RecoverFromUpdateValue(headerInformation.Round,
                        headerInformation.SenderPubkey.ToHex());

            if (headerInformation.Behaviour == AElfConsensusBehaviour.TinyBlock)
                headerInformation.Round =
                    currentRound.RecoverFromTinyBlock(headerInformation.Round,
                        headerInformation.SenderPubkey.ToHex());

            var isContainPreviousInValue = !currentRound.IsMinerListJustChanged;
            if (headerInformation.Round.GetHash(isContainPreviousInValue) !=
                currentRound.GetHash(isContainPreviousInValue))
            {
                var headerMiners = headerInformation.Round.RealTimeMinersInformation.Keys;
                var stateMiners = currentRound.RealTimeMinersInformation.Keys;
                var replacedMiners = headerMiners.Except(stateMiners).ToList();
                if (!replacedMiners.Any())
                    return new ValidationResult
                    {
                        Success = false, Message =
                            "Current round information is different with consensus extra data.\n" +
                            $"New block header consensus information:\n{headerInformation.Round}" +
                            $"Stated block header consensus information:\n{currentRound}"
                    };

                var newMiners = stateMiners.Except(headerMiners).ToList();
                var officialNewestMiners = replacedMiners.Select(miner =>
                        State.ElectionContract.GetNewestPubkey.Call(new StringValue { Value = miner }).Value)
                    .ToList();

                Assert(
                    newMiners.Count == officialNewestMiners.Count &&
                    newMiners.Union(officialNewestMiners).Count() == newMiners.Count,
                    "Incorrect replacement information.");
            }
        }

        return new ValidationResult { Success = true };
    }

    private TransactionList GenerateTransactionListByExtraData(AElfConsensusHeaderInformation consensusInformation,
        ByteString pubkey, ByteString randomNumber)
    {
        var round = consensusInformation.Round;
        var behaviour = consensusInformation.Behaviour;
        switch (behaviour)
        {
            case AElfConsensusBehaviour.UpdateValue:
                Context.LogDebug(() =>
                    $"Previous in value in extra data:{round.RealTimeMinersInformation[pubkey.ToHex()].PreviousInValue}");
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(nameof(UpdateValue),
                            round.ExtractInformationToUpdateConsensus(pubkey.ToHex(), randomNumber))
                    }
                };
            case AElfConsensusBehaviour.TinyBlock:
                var minerInRound = round.RealTimeMinersInformation[pubkey.ToHex()];
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(nameof(UpdateTinyBlockInformation),
                            new TinyBlockInput
                            {
                                ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
                                ProducedBlocks = minerInRound.ProducedBlocks,
                                RoundId = round.RoundIdForValidation,
                                RandomNumber = randomNumber
                            })
                    }
                };
            case AElfConsensusBehaviour.NextRound:
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(nameof(NextRound), NextRoundInput.Create(round,randomNumber))
                    }
                };
            case AElfConsensusBehaviour.NextTerm:
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(nameof(NextTerm), NextTermInput.Create(round,randomNumber))
                    }
                };
            default:
                return new TransactionList();
        }
    }
}