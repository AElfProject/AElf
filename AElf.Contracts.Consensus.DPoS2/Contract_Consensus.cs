using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class Contract : CSharpSmartContract<DPoSContractState>//, IConsensusSmartContract
    {
        [View]
        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            var dpoSInformation = DPoSInformation.Parser.ParseFrom(consensusInformation);
            if (TryToGetCurrentRoundInformation(out _))
            {
                if (dpoSInformation.MinersList.Any() && !IsMiner(dpoSInformation.Sender))
                {
                    return new ValidationResult {Success = false, Message = "Sender is not a miner."};
                }
            }
            else
            {
                if (dpoSInformation.MinersList.Any() && !dpoSInformation.MinersList.Contains(dpoSInformation.Sender))
                {
                    return new ValidationResult {Success = false, Message = "Sender is not a miner."};
                }
            }

            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                if (dpoSInformation.WillUpdateConsensus)
                {
                    if (dpoSInformation.Forwarding != null)
                    {
                        // Next Round
                        if (!MinersAreSame(currentRound, dpoSInformation.Forwarding.NextRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }

                        // Maybe it is acceptable to be null.
//                        if (!OutInValueAreNull(dpoSInformation.Forwarding.NextRound))
//                        {
//                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
//                        }

                        // TODO: Validate time slots (distance == 4000 ms)
                    }

                    if (dpoSInformation.NewTerm != null)
                    {
                        // Next Term
                        if (!ValidateVictories(dpoSInformation.NewTerm.Miners))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }

                        if (!OutInValueAreNull(dpoSInformation.NewTerm.FirstRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }

                        if (!OutInValueAreNull(dpoSInformation.NewTerm.SecondRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }

                        // TODO: Validate time slots (distance == 4000 ms)
                    }
                }
                else
                {
                    // Same Round
                    if (!RoundIdMatched(dpoSInformation.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    if (!NewOutValueFilled(dpoSInformation.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }
                }
            }

            return new ValidationResult {Success = true};
        }

        public int GetCountingMilliseconds(Timestamp timestamp)
        {
            // To initial this chain.
            if (!TryToGetCurrentRoundInformation(out var roundInformation))
            {
                return Config.InitialWaitingMilliseconds;
            }

            // To terminate current round.
            if ((AllOutValueFilled(out var minerInformation) || TimeOverflow(timestamp)) &&
                TryToGetMiningInterval(out var miningInterval))
            {
                var extraBlockMiningTime = roundInformation.GetEBPMiningTime(miningInterval);
                if (roundInformation.GetExtraBlockProducerInformation().PublicKey == Context.RecoverPublicKey().ToHex() &&
                    extraBlockMiningTime > timestamp.ToDateTime())
                {
                    return (int) (extraBlockMiningTime - timestamp.ToDateTime()).TotalMilliseconds;
                }

                var blockProducerNumber = roundInformation.RealTimeMinersInfo.Count;
                var roundTime = blockProducerNumber * miningInterval;
                var passedTime = (timestamp.ToDateTime() - extraBlockMiningTime).TotalMilliseconds % roundTime;
                if (passedTime > minerInformation.Order * miningInterval)
                {
                    return (int) (roundTime - (passedTime - minerInformation.Order * miningInterval));
                }

                return (int) (minerInformation.Order * miningInterval - passedTime);
            }

            // To produce a normal block.
            var expect = (int) (minerInformation.ExpectedMiningTime.ToDateTime() - timestamp.ToDateTime())
                .TotalMilliseconds;
            return expect >= 0 ? expect : int.MaxValue;
        }

        public IMessage GetNewConsensusInformation(byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            // To initial consensus information.
            if (!TryToGetRoundNumber(out _))
            {
                return new DPoSInformation
                {
                    Sender = Context.Sender,
                    WillUpdateConsensus = true,
                    NewTerm = extra.InitialMiners.ToMiners().GenerateNewTerm(extra.MiningInterval),
                    MinersList =
                        {extra.InitialMiners.Select(m => Address.FromPublicKey(ByteArrayHelpers.FromHexString(m)))},
                };
            }

            // To terminate current round.
            if (AllOutValueFilled(out _) || extra.Timestamp != null && TimeOverflow(extra.Timestamp))
            {
                return extra.ChangeTerm
                    ? new DPoSInformation
                    {
                        WillUpdateConsensus = true,
                        Sender = Context.Sender,
                        NewTerm = GenerateNextTerm(),
                    }
                    : new DPoSInformation
                    {
                        WillUpdateConsensus = true,
                        Sender = Context.Sender,
                        Forwarding = GenerateNewForwarding()
                    };
            }

            // To publish Out Value.
            return new DPoSInformation
            {
                CurrentRound = FillOutValue(extra.HashValue)
            };
        }

        public TransactionList GenerateConsensusTransactions(ulong refBlockHeight, byte[] refBlockPrefix,
            byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            // To initial consensus information.
            if (!TryToGetRoundNumber(out _))
            {
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "InitialTerm",
                            new List<object> {extra.NewTerm})
                    }
                };
            }

            // To terminate current round.
            if (AllOutValueFilled(out _) || extra.Timestamp != null && TimeOverflow(extra.Timestamp))
            {
                if (extra.ChangeTerm && TryToGetRoundNumber(out var roundNumber) &&
                    TryToGetTermNumber(out var termNumber))
                {
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(refBlockHeight, refBlockPrefix, "NextTerm",
                                new List<object> {extra.NewTerm}),
                            GenerateTransaction(refBlockHeight, refBlockPrefix, "SnapshotForMiners",
                                new List<object> {roundNumber, termNumber}),
                            GenerateTransaction(refBlockHeight, refBlockPrefix, "SnapshotForTerm",
                                new List<object> {roundNumber, termNumber}),
                            GenerateTransaction(refBlockHeight, refBlockPrefix, "SendDividends",
                                new List<object> {roundNumber, termNumber})
                        }
                    };
                }

                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "BroadcastInValue",
                            new List<object> {extra.ToBroadcast}),
                    }
                };
            }

            if (extra.InValue != null && extra.ToPackage != null)
            {
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "PackageOutValue",
                            new List<object> {extra.ToPackage}),
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "PublishInValue",
                            new List<object> {extra.InValue}),
                    }
                };
            }

            return new TransactionList();
        }

        public IMessage GetConsensusCommand(Timestamp timestamp)
        {
            // To initial this chain.
            if (!TryToGetCurrentRoundInformation(out var roundInformation))
            {
                return new DPoSCommand
                {
                    CountingMilliseconds = Config.InitialWaitingMilliseconds,
                    Behaviour = DPoSBehaviour.InitialTerm
                };
            }

            // To terminate current round.
            if ((AllOutValueFilled(out var minerInformation) || TimeOverflow(timestamp)) &&
                TryToGetMiningInterval(out var miningInterval))
            {
                var extraBlockMiningTime = roundInformation.GetEBPMiningTime(miningInterval);
                if (roundInformation.GetExtraBlockProducerInformation().PublicKey == Context.RecoverPublicKey().ToHex() &&
                    extraBlockMiningTime > timestamp.ToDateTime())
                {
                    return new DPoSCommand
                    {
                        CountingMilliseconds = (int) (extraBlockMiningTime - timestamp.ToDateTime()).TotalMilliseconds,
                        Behaviour = DPoSBehaviour.NextRound
                    };
                }

                var blockProducerNumber = roundInformation.RealTimeMinersInfo.Count;
                var roundTime = blockProducerNumber * miningInterval;
                var passedTime = (timestamp.ToDateTime() - extraBlockMiningTime).TotalMilliseconds % roundTime;
                if (passedTime > minerInformation.Order * miningInterval)
                {
                    return new DPoSCommand
                    {
                        CountingMilliseconds =
                            (int) (roundTime - (passedTime - minerInformation.Order * miningInterval)),
                        Behaviour = DPoSBehaviour.NextRound
                    };
                }

                return new DPoSCommand
                {
                    CountingMilliseconds = (int) (minerInformation.Order * miningInterval - passedTime),
                    Behaviour = DPoSBehaviour.NextRound
                };
            }

            // To produce a normal block.
            var expect = (int) (minerInformation.ExpectedMiningTime.ToDateTime() - timestamp.ToDateTime())
                .TotalMilliseconds;
            return new DPoSCommand
            {
                CountingMilliseconds = expect >= 0 ? expect : int.MaxValue,
                Behaviour = DPoSBehaviour.PackageOutValue
            };
        }
    }
}