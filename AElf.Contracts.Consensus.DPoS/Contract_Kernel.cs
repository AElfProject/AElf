using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable UnusedMember.Global
    public partial class ConsensusContract : CSharpSmartContract<DPoSContractState>, IConsensusSmartContract
    {
        public void Initialize(Address tokenContractAddress, Address dividendsContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.TokenContract.Value = tokenContractAddress;
            State.DividendContract.Value = dividendsContractAddress;
            State.Initialized.Value = true;
        }

        [View]
        public IMessage GetConsensusCommand(byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            var publicKey = extra.PublicKey;
            var timestamp = extra.Timestamp;
            
            TryToGetMiningInterval(out var miningInterval);

            // To initial this chain.
            if (!TryToGetCurrentRoundInformation(out var roundInformation))
            {
                return new ConsensusCommand
                {
                    CountingMilliseconds = extra.IsBootMiner ? DPoSContractConsts.AElfWaitFirstRoundTime : int.MaxValue,
                    TimeoutMilliseconds = int.MaxValue,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.InitialTerm
                    }.ToByteString()
                };
            }

            // To terminate current round.
            if (OwnOutValueFilled(publicKey, out var minerInformation) || TimeOverflow(timestamp))
            {
                // This node is Extra Block Producer of current round.
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

                // This node isn't EBP of current round.

                var blockProducerNumber = roundInformation.RealTimeMinersInfo.Count;
                var roundTime = blockProducerNumber * miningInterval;
                var passedTime = (timestamp.ToDateTime() - extraBlockMiningTime).TotalMilliseconds % roundTime;
                if (passedTime > minerInformation.Order * miningInterval)
                {
                    return new ConsensusCommand
                    {
                        CountingMilliseconds =
                            roundTime - ((int) passedTime - (minerInformation.Order * miningInterval)),
                        TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.NextRound
                        }.ToByteString()
                    };
                }

                return new ConsensusCommand
                {
                    CountingMilliseconds = (minerInformation.Order * miningInterval) - (int) passedTime,
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
                CountingMilliseconds = expect >= 0 ? expect : expect > -miningInterval ? 0 : int.MaxValue,
                TimeoutMilliseconds = miningInterval / minerInformation.PromisedTinyBlocks,
                Hint = new DPoSHint
                {
                    Behaviour = DPoSBehaviour.PackageOutValue
                }.ToByteString()
            };
        }

        [View]
        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);
            var publicKey = information.SenderPublicKey;

            // Validate the sender.
            if (!IsMiner(publicKey) && !information.MinersList.Contains(information.Sender))
            {
                return new ValidationResult {Success = false, Message = "Sender is not a miner."};
            }

            // Yes the consensus information exists in state database.
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
                        if (!InValueIsNull(information.Forwarding.NextRound))
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
                    }
                }
                else
                {
                    Console.WriteLine("Same round.");
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
                    SenderPublicKey = publicKey,
                    WillUpdateConsensus = true,
                    NewTerm = extra.InitialMiners.ToMiners().GenerateNewTerm(extra.MiningInterval),
                    MinersList =
                        {extra.InitialMiners.Select(m => Address.FromPublicKey(ByteArrayHelpers.FromHexString(m)))},
                    Behaviour = DPoSBehaviour.InitialTerm
                };
            }

            // To terminate current round.
            if (AllOutValueFilled(publicKey, out _) || (extra.Timestamp != null && TimeOverflow(extra.Timestamp)))
            {
                return extra.ChangeTerm
                    ? new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        WillUpdateConsensus = true,
                        Sender = Context.Sender,
                        NewTerm = GenerateNextTerm(),
                        Behaviour = DPoSBehaviour.NextTerm
                    }
                    : new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        WillUpdateConsensus = true,
                        Sender = Context.Sender,
                        Forwarding = GenerateNewForwarding(),
                        Behaviour = DPoSBehaviour.NextRound
                    };
            }

            // To publish Out Value.
            return new DPoSInformation
            {
                SenderPublicKey = publicKey,
                CurrentRound = FillOutValue(extra.OutValue, publicKey),
                Behaviour = DPoSBehaviour.PackageOutValue,
                
                Sender = Context.Sender
            };
        }

        [View]
        public TransactionList GenerateConsensusTransactions(ulong preBlockHeight, byte[] preBlockPrefix,
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
                        GenerateTransaction(preBlockHeight, preBlockPrefix, "InitialTerm",
                            new List<object> {extra.NewTerm})
                    }
                };
            }

            // To terminate current round.
            if (AllOutValueFilled(publicKey, out _) || (extra.Timestamp != null && TimeOverflow(extra.Timestamp)))
            {
                if (extra.ChangeTerm && TryToGetRoundNumber(out var roundNumber) &&
                    TryToGetTermNumber(out var termNumber))
                {
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(preBlockHeight, preBlockPrefix, "NextTerm",
                                new List<object> {extra.NewTerm}),
                            GenerateTransaction(preBlockHeight, preBlockPrefix, "SnapshotForMiners",
                                new List<object> {roundNumber, termNumber}),
                            GenerateTransaction(preBlockHeight, preBlockPrefix, "SnapshotForTerm",
                                new List<object> {roundNumber, termNumber}),
                            GenerateTransaction(preBlockHeight, preBlockPrefix, "SendDividends",
                                new List<object> {roundNumber, termNumber})
                        }
                    };
                }

                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(preBlockHeight, preBlockPrefix, "NextRound",
                            new List<object> {extra.Forwarding})
                    }
                };
            }

            if (extra.ToBroadcast != null && extra.ToPackage != null)
            {
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(preBlockHeight, preBlockPrefix, "PackageOutValue",
                            new List<object> {extra.ToPackage}),
                        GenerateTransaction(preBlockHeight, preBlockPrefix, "BroadcastInValue",
                            new List<object> {extra.ToBroadcast}),
                    }
                };
            }

            return new TransactionList();
        }
    }
}