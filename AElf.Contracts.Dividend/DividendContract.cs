using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Dividend
{
    public partial class DividendContract : DividendContractContainer.DividendContractBase
    {
        public override Empty InitializeDividendContract(InitialDividendContractInput input)
        {
            var consensusContractAddress = input.ConsensusContractSystemName;
            var tokenContractSystemName = input.TokenContractSystemName;
            Assert(!State.Initialized.Value, "Already initialized.");
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.TokenContractSystemName.Value = tokenContractSystemName;
            State.ConsensusContractSystemName.Value = consensusContractAddress;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty SendDividends(SendDividendsInput input)
        {
            var targetAddress = input.To;
            var amount = input.Amount;
            if (amount <= 0)
                return new Empty();

            if (State.ConsensusContract.Value == null)
            {
                State.ConsensusContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);
            }
            
            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can transfer dividends.");

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            }
            
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = targetAddress,
                Amount = amount,
                Symbol = "ELF",
                Memo = "Send dividends."
            });
            return new Empty();
        }

        /// <summary>
        /// Transfer dividends to miners.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public override SInt64Value TransferDividends(VotingRecord input)
        {
            var votingRecord = input;
            if (State.ConsensusContract.Value == null)
            {
                State.ConsensusContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);
            }
            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can transfer dividends.");

            var dividendsOwner = votingRecord.From;
            var dividendsOwnerAddress = Address.FromPublicKey(ByteArrayHelpers.FromHexString(dividendsOwner));

            var startTermNumber = votingRecord.TermNumber + 1;
            var history = State.LastRequestedDividendsMap[votingRecord.TransactionId];
            if (history > 0)
            {
                startTermNumber = history + 1;
            }

            var voteInfo= new VoteInfo()
            {
                Record = votingRecord,
                Age = State.ConsensusContract.GetBlockchainAge.Call(new Empty()).Value
            };
            var endTermNumber = Math.Min(GetExpireTermNumber(voteInfo).Value,
                State.ConsensusContract.GetCurrentTermNumber.Call(new Empty()).Value - 1);

            // Record last requested dividends term number.
            var actualTermNumber = startTermNumber;
            long totalDividendsAmount = 0;
            for (var i = startTermNumber; i <= endTermNumber; i++)
            {
                var totalWeights = State.TotalWeightsMap[i];
                Assert(totalWeights > 0, $"Total weights of term {i} not found.");
                var dividends = State.DividendsMap[i];
                Assert(dividends > 0, $"Dividends of term {i} not found.");
                totalDividendsAmount += dividends * votingRecord.Weight / totalWeights;
                actualTermNumber = i;
            }

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            }
            
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = dividendsOwnerAddress,
                Amount = totalDividendsAmount,
                Symbol = "ELF",
                Memo = "Transfer dividends."
            });

            Context.LogDebug(()=>$"Gonna transfer {totalDividendsAmount} dividends to {dividendsOwnerAddress}");

            State.LastRequestedDividendsMap[votingRecord.TransactionId] = actualTermNumber;

            return new SInt64Value() {Value = totalDividendsAmount};
        }

        public override SInt64Value AddDividends(AddDividendsInput input)
        {
            var termNumber = input.TermNumber;
            var dividendsAmount = input.DividendsAmount;
            var currentDividends = State.DividendsMap[termNumber];
            var finalDividends = currentDividends + dividendsAmount;
            State.DividendsMap[termNumber] = finalDividends;
            Context.LogDebug(()=>$"Dividends of term {termNumber}: {dividendsAmount}");

            return new SInt64Value() {Value = finalDividends};
        }

        public override SInt64Value AddWeights(WeightsInfo input)
        {
            var termNumber = input.TermNumber;
            var weights = input.Weights;
            var currentWeights = State.TotalWeightsMap[termNumber];
            var finalWeights = currentWeights + weights;
            State.TotalWeightsMap[termNumber] = finalWeights;
            Context.LogDebug(()=>$"Weights of term {termNumber}: {finalWeights}.[Add]");

            return new SInt64Value() {Value = finalWeights};
        }

        public override ActionResult KeepWeights(SInt64Value input)
        {
            var oldTermNumber = input.Value;
            var totalWeights = State.TotalWeightsMap[oldTermNumber];
            if (totalWeights > 0)
            {
                Context.LogDebug(()=>"[Forwarding weights]");
                AddWeights(new WeightsInfo()
                {
                    TermNumber = oldTermNumber + 1,
                    Weights = totalWeights
                });
            }

            return new ActionResult {Success = true};
        }

        public override ActionResult SubWeights(WeightsInfo input)
        {
            var termNumber = input.TermNumber;
            var weights = input.Weights;
            var totalWeights = State.TotalWeightsMap[termNumber];
            Assert(totalWeights > 0, $"Invalid weights of term {termNumber}");
            var newWeights = totalWeights - weights;
            State.TotalWeightsMap[termNumber] = newWeights;

            return new ActionResult {Success = true};
        }
    }
}