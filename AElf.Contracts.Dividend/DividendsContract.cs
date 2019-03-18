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
    public partial class DividendsContract : CSharpSmartContract<DividendsContractState>
    {
        public void Initialize(Address consensusContractAddress, Address tokenContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContract.Value = consensusContractAddress;
            State.TokenContract.Value = tokenContractAddress;
            State.Initialized.Value = true;
            State.StarterPublicKey.Value = Context.RecoverPublicKey().ToHex();
        }

        public void SendDividends(Address targetAddress, long amount)
        {
            if (amount <= 0)
                return;

            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can transfer dividends.");

            State.TokenContract.Transfer(new TransferInput
            {
                To = targetAddress,
                Amount = amount,
                Symbol = "ELF",
                Memo = "Send dividends."
            });
        }
        
        /// <summary>
        /// Transfer dividends to miners.
        /// </summary>
        /// <param name="votingRecord"></param>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public long TransferDividends(VotingRecord votingRecord)
        {
            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can transfer dividends.");

            var dividendsOwner = votingRecord.From;
            var dividendsOwnerAddress = Address.FromPublicKey(ByteArrayHelpers.FromHexString(dividendsOwner));

            var startTermNumber = votingRecord.TermNumber + 1;
            var history = State.LastRequestedDividendsMap[votingRecord.TransactionId];
            if (history > 0)
            {
                startTermNumber = history + 1;
            }

            var endTermNumber = Math.Min(GetExpireTermNumber(votingRecord, State.ConsensusContract.GetBlockchainAge()),
                State.ConsensusContract.GetCurrentTermNumber() - 1);

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

            State.TokenContract.Transfer(new TransferInput
            {
                To = dividendsOwnerAddress,
                Amount = totalDividendsAmount,
                Symbol = "ELF",
                Memo = "Transfer dividends."
            });

            Context.LogDebug(()=>$"Gonna transfer {totalDividendsAmount} dividends to {dividendsOwnerAddress}");

            State.LastRequestedDividendsMap[votingRecord.TransactionId] = actualTermNumber;

            return totalDividendsAmount;
        }

        public long AddDividends(long termNumber, long dividendsAmount)
        {
            var currentDividends = State.DividendsMap[termNumber];
            var finalDividends = currentDividends + dividendsAmount;
            State.DividendsMap[termNumber] = finalDividends;
            Context.LogDebug(()=>$"Dividends of term {termNumber}: {dividendsAmount}");

            return finalDividends;
        }

        public long AddWeights(long weights, long termNumber)
        {
            var currentWeights = State.TotalWeightsMap[termNumber];
            var finalWeights = currentWeights + weights;
            State.TotalWeightsMap[termNumber] = finalWeights;
            Context.LogDebug(()=>$"Weights of term {termNumber}: {finalWeights}.[Add]");

            return finalWeights;
        }

        public ActionResult KeepWeights(long oldTermNumber)
        {
            var totalWeights = State.TotalWeightsMap[oldTermNumber];
            if (totalWeights > 0)
            {
                Context.LogDebug(()=>"[Forwarding weights]");
                AddWeights(totalWeights, oldTermNumber + 1);
            }

            return new ActionResult {Success = true};
        }

        public ActionResult SubWeights(long weights, long termNumber)
        {
            var totalWeights = State.TotalWeightsMap[termNumber];
            Assert(totalWeights > 0, $"Invalid weights of term {termNumber}");
            var newWeights = totalWeights - weights;
            State.TotalWeightsMap[termNumber] = newWeights;

            return new ActionResult {Success = true};
        }
    }
}