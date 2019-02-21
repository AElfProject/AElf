using System;
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
                        // Compare current round information from State Database and next round information from block header.
                        if (!ValidateMinersList(currentRound, information.Forwarding.NextRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }

                        // None of in values should be filled.
                        if (InValueIsNull(information.Forwarding.CurrentRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect in values."};
                        }
                    }

                    if (information.NewTerm != null)
                    {
                        // Will go next term.
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
            var publicKey = extra.PublicKey;

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
            if (AllOutValueFilled(publicKey, out _) || extra.Timestamp != null && TimeOverflow(extra.Timestamp))
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
                CurrentRound = FillOutValue(extra.HashValue, publicKey),
                Behaviour = DPoSBehaviour.PackageOutValue
            };
        }

        [View]
        public TransactionList GenerateConsensusTransactions(ulong refBlockHeight, byte[] refBlockPrefix,
            byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);
            var publicKey = extra.PublicKey;

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
            if (AllOutValueFilled(publicKey, out _) || extra.Timestamp != null && TimeOverflow(extra.Timestamp))
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
            }

            if (extra.ToBroadcast != null && extra.ToPackage != null)
            {
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "PackageOutValue",
                            new List<object> {extra.ToPackage}),
                        GenerateTransaction(refBlockHeight, refBlockPrefix, "BroadcastInValue",
                            new List<object> {extra.ToBroadcast}),
                    }
                };
            }

            return new TransactionList();
        }

        [View]
        public IMessage GetConsensusCommand(Timestamp timestamp, string publicKey)
        {
            
            TryToGetMiningInterval(out var miningInterval);
            
            // To initial this chain.
            if (!TryToGetCurrentRoundInformation(out var roundInformation))
            {
                return new ConsensusCommand
                {
                    CountingMilliseconds = Config.InitialWaitingMilliseconds,
                    TimeoutMilliseconds = miningInterval,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.InitialTerm
                    }.ToByteString()
                };
            }

            // To terminate current round.
            if (OwnOutValueFilled(publicKey, out var minerInformation) || TimeOverflow(timestamp))
            {
                var extraBlockMiningTime = roundInformation.GetExtraBlockMiningTime(miningInterval);
                if (roundInformation.GetExtraBlockProducerInformation().PublicKey == publicKey &&
                    extraBlockMiningTime > timestamp.ToDateTime())
                {
                    return new ConsensusCommand
                    {
                        CountingMilliseconds = (int) (extraBlockMiningTime - timestamp.ToDateTime()).TotalMilliseconds,
                        TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.NextRound
                        }.ToByteString()
                    };
                }

                var blockProducerNumber = roundInformation.RealTimeMinersInfo.Count;
                var roundTime = blockProducerNumber * miningInterval;
                var passedTime = (timestamp.ToDateTime() - extraBlockMiningTime).TotalMilliseconds % roundTime;
                if (passedTime > minerInformation.Order * miningInterval)
                {
                    return new ConsensusCommand
                    {
                        CountingMilliseconds =
                            (int) (roundTime - (passedTime - minerInformation.Order * miningInterval)),
                        TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.NextRound
                        }.ToByteString()
                    };
                }

                return new ConsensusCommand
                {
                    CountingMilliseconds = (int) (minerInformation.Order * miningInterval - passedTime),
                    TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.NextRound
                    }.ToByteString()
                };
            }

            // To produce a normal block.
            var expect = (int) (minerInformation.ExpectedMiningTime.ToDateTime() - timestamp.ToDateTime())
                .TotalMilliseconds;
            return new ConsensusCommand
            {
                CountingMilliseconds = expect >= 0 ? expect : int.MaxValue,
                TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                Hint = new DPoSHint
                {
                    Behaviour = DPoSBehaviour.PackageOutValue
                }.ToByteString()
            };
        }
    }
}