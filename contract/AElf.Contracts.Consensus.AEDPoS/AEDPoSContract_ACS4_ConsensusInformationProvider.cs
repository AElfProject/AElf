using System;
using System.Linq;
using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// In this method, `Context.CurrentBlockTime` is the time one miner start request his next consensus command.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            _processingBlockMinerPubkey = input.Value.ToHex();

            if (Context.CurrentHeight < 2) return ConsensusCommandProviderBase.InvalidConsensusCommand;

            if (!TryToGetCurrentRoundInformation(out var currentRound))
                return ConsensusCommandProviderBase.InvalidConsensusCommand;

            if (!currentRound.IsInMinerList(_processingBlockMinerPubkey))
                return ConsensusCommandProviderBase.InvalidConsensusCommand;

            TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp);

            var behaviour = IsMainChain
                ? new MainChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey, GetMaximumBlocksCount(),
                        Context.CurrentBlockTime, blockchainStartTimestamp, State.TimeEachTerm.Value)
                    .GetConsensusBehaviour()
                : new SideChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey, GetMaximumBlocksCount(),
                    Context.CurrentBlockTime).GetConsensusBehaviour();

            Context.LogDebug(() => $"{currentRound.ToString(_processingBlockMinerPubkey)}\nCurrent behaviour: {behaviour.ToString()}");

            return behaviour == AElfConsensusBehaviour.Nothing
                ? ConsensusCommandProviderBase.InvalidConsensusCommand
                //: new ConsensusCommandProviderBase().GetConsensusCommand();
                : GetConsensusCommand(behaviour, currentRound, _processingBlockMinerPubkey);
        }

        public override BytesValue GetInformationToUpdateConsensus(BytesValue input)
        {
            return GetConsensusBlockExtraData(input);
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);
            // Some basic checks.
            Assert(triggerInformation.Pubkey.Any(),
                "Data to request consensus information should contain public key.");

            var publicKey = triggerInformation.Pubkey;
            var consensusInformation = new AElfConsensusHeaderInformation();
            consensusInformation.MergeFrom(GetConsensusBlockExtraData(input, true).Value);
            var round = consensusInformation.Round;
            var behaviour = consensusInformation.Behaviour;
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(publicKey.ToHex()))
                        }
                    };
                case AElfConsensusBehaviour.TinyBlock:
                    var minerInRound = round.RealTimeMinersInformation[publicKey.ToHex()];
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateTinyBlockInformation),
                                new TinyBlockInput
                                {
                                    ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
                                    ProducedBlocks = minerInRound.ProducedBlocks,
                                    RoundId = round.RoundId
                                })
                        }
                    };
                case AElfConsensusBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound), round)
                        }
                    };
                case AElfConsensusBehaviour.NextTerm:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextTerm), round)
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            var extraData = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value.ToByteArray());
            return ValidateBeforeExecution(extraData);
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input1)
        {
            var input = new AElfConsensusHeaderInformation();
            input.MergeFrom(input1.Value);
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var isContainPreviousInValue =
                    input.Behaviour != AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                if (input.Round.GetHash(isContainPreviousInValue) != currentRound.GetHash(isContainPreviousInValue))
                {
                    Context.LogDebug(() => $"Round information of block header:\n{input.Round}");
                    Context.LogDebug(() => $"Round information of executing result:\n{currentRound}");
                    return new ValidationResult
                    {
                        Success = false, Message = "Current round information is different with consensus extra data."
                    };
                }
            }

            return new ValidationResult {Success = true};
        }
    }
}