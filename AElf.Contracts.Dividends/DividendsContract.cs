using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Dividends
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
        
        public ActionResult TransferDividends(VotingRecord votingRecord)
        {
            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can add dividends.");

            var owner = votingRecord.From;
            var ownerAddress =
                Address.FromPublicKey(ByteArrayHelpers.FromHexString(owner));

            var start = votingRecord.TermNumber + 1;
            var history = State.LastRequestDividendsMap[votingRecord.TransactionId];
            if (history > 0)
            {
                start = history + 1;
            }

            var end = Math.Min(GetExpireTermNumber(votingRecord, State.ConsensusContract.GetBlockchainAge()),
                State.ConsensusContract.GetCurrentTermNumber() - 1);

            var actualTermNumber = start;
            ulong dividendsAmount = 0;
            for (var i = start; i <= end; i++)
            {
                var totalWeights = State.TotalWeightsMap[i];
                if (totalWeights > 0)
                {
                    var dividends = State.DividendsMap[i];
                    if (dividends > 0)
                    {
                        dividendsAmount += dividends * votingRecord.Weight / totalWeights;
                        actualTermNumber = i;
                    }
                    else
                    {
                        return new ActionResult {Success = false, ErrorMessage = $"Dividends of term {i} not found."};
                    }
                }
                else
                {
                    return new ActionResult {Success = false, ErrorMessage = $"Total weights of term {i} not found."};
                }
            }

            State.TokenContract.Transfer(ownerAddress, dividendsAmount);

            Context.LogDebug(()=>$"Gonna transfer {dividendsAmount} dividends to {ownerAddress}");

            State.LastRequestDividendsMap[votingRecord.TransactionId] = actualTermNumber;

            return new ActionResult {Success = true};
        }

        public ActionResult AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            var dividends = State.DividendsMap[termNumber];

            Assert(Context.Sender == State.ConsensusContract.Value, "Only consensus contract can add dividends.");
            
            if (dividends > 0)
            {
                var finalDividends = dividends + dividendsAmount;
                State.DividendsMap[termNumber] = finalDividends;
            }
            else
            {
                State.DividendsMap[termNumber] = dividendsAmount;
            }

            Context.LogDebug(()=>$"Dividends of term {termNumber}: {dividendsAmount}");

            return new ActionResult {Success = true};
        }

        public ActionResult AddWeights(ulong weights, ulong termNumber)
        {
            var totalWeights = State.TotalWeightsMap[termNumber];
            if (totalWeights > 0)
            {
                var finalWeights = totalWeights + weights;
                State.TotalWeightsMap[termNumber] = finalWeights;
                Context.LogDebug(()=>$"Weights of term {termNumber}: {finalWeights}.[Add]");
            }
            else
            {
                State.TotalWeightsMap[termNumber] = weights;
                Context.LogDebug(()=>$"Weights of term {termNumber}: {weights}.[Add]");
            }

            return new ActionResult {Success = true};
        }

        public ActionResult KeepWeights(ulong oldTermNumber)
        {
            var totalWeights = State.TotalWeightsMap[oldTermNumber];
            if (totalWeights > 0)
            {
                Context.LogDebug(()=>"[Forwarding weights]");
                AddWeights(totalWeights, oldTermNumber + 1);
            }

            return new ActionResult {Success = true};
        }

        public ActionResult SubWeights(ulong weights, ulong termNumber)
        {
            var totalWeights = State.TotalWeightsMap[termNumber];
            Assert(totalWeights > 0, $"Invalid weights of term {termNumber}");
            var newWeights = totalWeights - weights;
            State.TotalWeightsMap[termNumber] = newWeights;

            return new ActionResult {Success = true};
        }
    }
}