using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Dividends2
{
    public class DividendsContract : CSharpSmartContract<DividendsContractState>
    {
        public void Initialize(Address consensusContractAddress, Address tokenContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContract.Value = consensusContractAddress;
            State.TokenContract.Value = tokenContractAddress;
            State.Initialized.Value = true;
        }
        [View]
        public ulong GetTermDividends(ulong termNumber)
        {
            return State.DividendsMap[termNumber];
        }

        [View]
        public ulong GetTermTotalWeights(ulong termNumber)
        {
            return State.TotalWeightsMap[termNumber];
        }

        [View]
        public ulong GetLatestRequestDividendsTermNumber(VotingRecord votingRecord)
        {
            var termNumber = State.LastRequestDividendsMap[votingRecord.TransactionId];
            return termNumber != 0
                ? termNumber
                : votingRecord.TermNumber;
        }

        [View]
        public ulong GetAvailableDividends(VotingRecord votingRecord)
        {
            // todo: Disable temporarily
//            ulong dividends = 0;
//
//            var start = votingRecord.TermNumber + 1;
//            var lastRequestTermNumber = State.LastRequestDividendsMap[votingRecord.TransactionId];
//            if (lastRequestTermNumber > 0)
//            {
//                start = lastRequestTermNumber + 1;
//            }
//
//            var end = Math.Min(votingRecord.GetExpireTermNumber(State.ConsensusContract.GetBlockchainAge()),
//                State.ConsensusContract.GetCurrentTermNumber() - 1);
//
//            for (var i = start; i <= end; i++)
//            {
//                var totalWeights = State.TotalWeightsMap[i];
//                if (totalWeights > 0)
//                {
//                    var totalDividends = State.DividendsMap[i];
//                    if (totalDividends > 0)
//                    {
//                        // TODO: No console write in contract
//                        Console.WriteLine($"Getting dividends of {votingRecord.TransactionId.ToHex()}: ");
//                        Console.WriteLine($"Total weights of term {i}: {totalWeights}");
//                        Console.WriteLine($"Total dividends of term {i}: {totalDividends}");
//                        Console.WriteLine($"Weights of this vote: {votingRecord.Weight}");
//                        dividends += totalDividends * votingRecord.Weight / totalWeights;
//                        Console.WriteLine($"Result: {dividends}");
//                    }
//                }
//            }
//
//            return dividends;
        }

        [View]
        public ulong GetAllAvailableDividends(string publicKey)
        {
            return State.ConsensusContract.GetTicketsInfo(publicKey).VotingRecords
                .Where(vr => vr.From == publicKey)
                .Aggregate<VotingRecord, ulong>(0,
                    (current, votingRecord) => current + GetAvailableDividends(votingRecord));
        }

        [View]
        public ulong CheckDividends(ulong ticketsAmount, int lockTime, ulong termNumber)
        {
            var currentTermNumber = State.ConsensusContract.GetCurrentTermNumber();
            Assert(termNumber <= currentTermNumber, "Cannot check dividends of future term.");
            var totalWeights = State.TotalWeightsMap[termNumber];
            if (totalWeights > 0)
            {
                var totalDividends = State.DividendsMap[termNumber];
                if (totalDividends > 0)
                {
                    return VotingRecord.CalculateWeight(ticketsAmount, lockTime) * totalDividends /
                           totalWeights;
                }
            }

            return 0;
        }

        [View]
        public ULongList CheckDividendsOfPreviousTerm()
        {
            var termNumber = State.ConsensusContract.GetCurrentTermNumber() - 1;
            var result = new ULongList();

            if (termNumber < 1)
            {
                return new ULongList {Values = {0}, Remark = "Not found."};
            }

            const ulong ticketsAmount = 10_000;
            var lockTimes = new List<int> {30, 180, 365, 730, 1095};
            foreach (var lockTime in lockTimes)
            {
                result.Values.Add(CheckDividends(ticketsAmount, lockTime, termNumber));
            }

            return result;
        }

        [View]
        public string CheckDividendsOfPreviousTermToFriendlyString()
        {
            return CheckDividendsOfPreviousTerm().ToString();
        }

        public ActionResult TransferDividends(VotingRecord votingRecord)
        {
            // todo: Disable temporarily
//            var owner = votingRecord.From;
//            var ownerAddress =
//                Address.FromPublicKey(ByteArrayHelpers.FromHexString(owner));
//
//            var start = votingRecord.TermNumber + 1;
//            var history = State.LastRequestDividendsMap[votingRecord.TransactionId];
//            if (history > 0)
//            {
//                start = history + 1;
//            }
//
//            var end = Math.Min(votingRecord.GetExpireTermNumber(State.ConsensusContract.GetBlockchainAge()),
//                State.ConsensusContract.GetCurrentTermNumber() - 1);
//
//            var actualTermNumber = start;
//            ulong dividendsAmount = 0;
//            for (var i = start; i <= end; i++)
//            {
//                var totalWeights = State.TotalWeightsMap[i];
//                if (totalWeights > 0)
//                {
//                    var dividends = State.DividendsMap[i];
//                    if (dividends > 0)
//                    {
//                        dividendsAmount += dividends * votingRecord.Weight / totalWeights;
//                        actualTermNumber = i;
//                    }
//                    else
//                    {
//                        return new ActionResult {Success = false, ErrorMessage = $"Dividends of term {i} not found."};
//                    }
//                }
//                else
//                {
//                    return new ActionResult {Success = false, ErrorMessage = $"Total weights of term {i} not found."};
//                }
//            }
//
//            State.TokenContract.Transfer(ownerAddress, dividendsAmount);
//
//            Console.WriteLine($"Gonna transfer {dividendsAmount} dividends to {ownerAddress}");
//
//            State.LastRequestDividendsMap[votingRecord.TransactionId] = actualTermNumber;
//
//            return new ActionResult {Success = true};
        }

        public ActionResult AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            var dividends = State.DividendsMap[termNumber];
            if (dividends > 0 && Context.Sender != State.ConsensusContract.Value)
            {
                var finalDividends = dividends + dividendsAmount;
                State.DividendsMap[termNumber] = finalDividends;
            }
            else
            {
                State.DividendsMap[termNumber] = dividendsAmount;
            }

            Console.WriteLine($"Dividends of term {termNumber}: {dividendsAmount}");

            return new ActionResult {Success = true};
        }

        public ActionResult AddWeights(ulong weights, ulong termNumber)
        {
            var totalWeights = State.TotalWeightsMap[termNumber];
            if (totalWeights > 0)
            {
                var finalWeights = totalWeights + weights;
                State.TotalWeightsMap[termNumber] = finalWeights;
                Console.WriteLine($"Weights of term {termNumber}: {finalWeights}.[Add]");
            }
            else
            {
                State.TotalWeightsMap[termNumber] = weights;
                Console.WriteLine($"Weights of term {termNumber}: {weights}.[Add]");
            }

            return new ActionResult {Success = true};
        }

        public ActionResult KeepWeights(ulong oldTermNumber)
        {
            var totalWeights = State.TotalWeightsMap[oldTermNumber];
            if (totalWeights > 0)
            {
                Console.WriteLine("[Forwarding weights]");
                AddWeights(totalWeights, oldTermNumber + 1);
            }

            return new ActionResult {Success = true};
        }

        public ActionResult SubWeights(ulong weights, ulong termNumber)
        {
            var totalWeights = State.TotalWeightsMap[termNumber];
            if (totalWeights > 0)
            {
                var newWeights = totalWeights - weights;
                State.TotalWeightsMap[termNumber] = newWeights;
                Console.WriteLine($"Weights of term {termNumber}: {totalWeights}.[Sub]");
            }

            return new ActionResult {Success = true};
        }
    }
}