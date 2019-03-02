using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable UnusedMember.Global
    public partial class ConsensusContract : CSharpSmartContract<DPoSContractState>, IConsensusSmartContract
    {
        // This file contains implementations of IConsensusSmartContract.
        
        public void Initialize(Address tokenContractAddress, Address dividendsContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.TokenContract.Value = tokenContractAddress;
            State.DividendContract.Value = dividendsContractAddress;
            State.Initialized.Value = true;
        }

        [View]
        public IMessage GetConsensusCommand(byte[] consensusTriggerInformation)
        {
            var triggerInformation = DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);

            // Some basic checks.
            Assert(triggerInformation.PublicKey.Any(), "Trigger information should contain public key.");
            Assert(triggerInformation.Timestamp.IsNotEmpty(), "Trigger information should contain timestamp.");

            var publicKey = triggerInformation.PublicKey;
            var timestamp = triggerInformation.Timestamp;

            // If we can't get current round information from state db, it means this chain hasn't initialized yet,
            // so the context of current command is to initial a new chain via creating the consensus initial information.
            // And to initial DPoS information, we need to generate the information of first round, at least.
            if (!TryToGetCurrentRoundInformation(out var round))
            {
                Context.LogDebug(() => "About to initial DPoS information.");
                return new ConsensusCommand
                {
                    // For now, only if one node configured himself as a boot miner can he actually create the first block,
                    // which block height is 2.
                    CountingMilliseconds =
                        triggerInformation.IsBootMiner ? triggerInformation.MiningInterval : int.MaxValue,
                    // No need to limit the mining time for the first block a chain.
                    TimeoutMilliseconds = int.MaxValue,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.InitialTerm
                    }.ToByteString()
                };
            }

            Assert(TryToGetMiningInterval(out var miningInterval), "Failed to get mining interval.");

            // If this node missed his time slot, a command of terminating current round will be fired,
            // and the terminate time will based on the order of this node (to avoid conflicts).
            if (round.IsTimeSlotPassed(publicKey, timestamp, out var minerInRound))
            {
                // TODO: Add a test case to test the ability to mine a block even this miner missed his time slot long time ago.
                
                Context.LogDebug(() => "About to terminate current round.");

                var arrangedMiningTime = round.ArrangeAbnormalMiningTime(publicKey, timestamp);
                var countingMilliseconds = (int) (arrangedMiningTime.ToDateTime() - timestamp.ToDateTime()).TotalMilliseconds;
                return new ConsensusCommand
                {
                    CountingMilliseconds = countingMilliseconds,
                    TimeoutMilliseconds = miningInterval / minerInRound.PromisedTinyBlocks,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.NextRound
                    }.ToByteString()
                };
            }
            else
            {
                Context.LogDebug(() => "About to produce a normal block.");
                
                var expectedMiningTime = round.GetExpectedMiningTime(publicKey);
                var countingMilliseconds = (int) (expectedMiningTime.ToDateTime() - timestamp.ToDateTime()).TotalMilliseconds;
                return new ConsensusCommand
                {
                    CountingMilliseconds = countingMilliseconds,
                    TimeoutMilliseconds = miningInterval / minerInRound.PromisedTinyBlocks,
                    Hint = new DPoSHint
                    {
                        Behaviour = DPoSBehaviour.PackageOutValue
                    }.ToByteString()
                };
            }
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

            var signature = Hash.Generate();
            if (TryToGetPreviousRoundInformation(out var preRoundInformation) && extra.CurrentInValue != null)
            {
                signature = preRoundInformation.CalculateSignature(extra.CurrentInValue);
            }

            // To publish Out Value.
            return new DPoSInformation
            {
                SenderPublicKey = publicKey,
                CurrentRound = FillOutValueAndSignature(extra.OutValue, signature, publicKey),
                Behaviour = DPoSBehaviour.PackageOutValue,
                Sender = Context.Sender
            };
        }

        [View]
        public TransactionList GenerateConsensusTransactions(byte[] extraInformation)
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
                        GenerateTransaction("InitialTerm", new List<object> {extra.NewTerm})
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
                            GenerateTransaction("NextTerm", new List<object> {extra.NewTerm}),
                            GenerateTransaction("SnapshotForMiners", new List<object> {roundNumber, termNumber}),
                            GenerateTransaction("SnapshotForTerm", new List<object> {roundNumber, termNumber}),
                            GenerateTransaction("SendDividends", new List<object> {roundNumber, termNumber})
                        }
                    };
                }

                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction("NextRound", new List<object> {extra.Forwarding})
                    }
                };
            }

            if (extra.ToBroadcast != null && extra.ToPackage != null)
            {
                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction("PackageOutValue", new List<object> {extra.ToPackage}),
                        GenerateTransaction("BroadcastInValue", new List<object> {extra.ToBroadcast}),
                    }
                };
            }

            return new TransactionList();
        }
    }
}