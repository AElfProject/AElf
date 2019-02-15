using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class Contract : CSharpSmartContract<DPoSContractState>, IConsensusSmartContract
    {
        public void Initialize(Address tokenContractAddress, Address dividendsContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.TokenContract.Value = tokenContractAddress;
            State.DividendContract.Value = dividendsContractAddress;
            State.Initialized.Value = true;
        }

        [View]
        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            // Validate the sender.
            if (!IsMiner(information.Sender) || !information.MinersList.Contains(information.Sender))
            {
                return new ValidationResult {Success = false, Message = "Sender is not a miner."};
            }

            // Yes the consensus information exists.
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                // The mining process will go next round or next term.
                if (information.WillUpdateConsensus)
                {
                    // Will go next round.
                    if (information.Forwarding != null)
                    {
                        if (!ValidateMinersList(currentRound, information.Forwarding.NextRound))
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

                    if (information.NewTerm != null)
                    {
                        // Next Term
                        if (!ValidateVictories(information.NewTerm.Miners))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }

                        if (!OutInValueAreNull(information.NewTerm.FirstRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }

                        if (!OutInValueAreNull(information.NewTerm.SecondRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }

                        // TODO: Validate time slots (distance == 4000 ms)
                    }
                }
                else
                {
                    // Same Round
                    if (!RoundIdMatched(information.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    if (!NewOutValueFilled(information.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }
                }
            }

            return new ValidationResult {Success = true};
        }

        [View]
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
                    Behaviour = DPoSBehaviour.InitialTerm
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
                        Behaviour = DPoSBehaviour.NextTerm
                    }
                    : new DPoSInformation
                    {
                        WillUpdateConsensus = true,
                        Sender = Context.Sender,
                        Forwarding = GenerateNewForwarding(),
                        Behaviour = DPoSBehaviour.NextRound
                    };
            }

            // To publish Out Value.
            return new DPoSInformation
            {
                CurrentRound = FillOutValue(extra.HashValue),
                Behaviour = DPoSBehaviour.PackageOutValue
            };
        }

        [View]
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

        [View]
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
                if (roundInformation.GetExtraBlockProducerInformation().PublicKey ==
                    Context.RecoverPublicKey().ToHex() &&
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