using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Dividends
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class DividendsContract : CSharpSmartContract
    {
        // Term Number -> Dividends Amount
        private readonly Map<UInt64Value, UInt64Value> _dividendsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.DividendsMapString);

        // Term Number -> Total weights
        private readonly Map<UInt64Value, UInt64Value> _totalWeightsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.TotalWeightsMapString);

        // Because voter can request dividends of each VotingRecord instance for terms it experienced,
        // we need to record the term number of last term he request his dividends.
        // Hash (of VotingRecord) -> Latest request dividends term number
        private readonly Map<Hash, UInt64Value> _lastRequestDividendsMap =
            new Map<Hash, UInt64Value>(GlobalConfig.TransferMapString);

        [View]
        public ulong GetTermDividends(ulong termNumber)
        {
            return _dividendsMap.TryGet(termNumber.ToUInt64Value(), out var dividends) ? dividends.Value : 0;
        }

        [View]
        public ulong GetTermTotalWeights(ulong termNumber)
        {
            return _totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var weights) ? weights.Value : 0;
        }

        [View]
        public ulong GetLatestRequestDividendsTermNumber(VotingRecord votingRecord)
        {
            return _lastRequestDividendsMap.TryGet(votingRecord.TransactionId, out var termNumber)
                ? termNumber.Value
                : votingRecord.TermNumber;
        }

        [View]
        public ulong GetAvailableDividends(VotingRecord votingRecord)
        {
            ulong dividends = 0;

            var start = votingRecord.TermNumber + 1;
            if (_lastRequestDividendsMap.TryGet(votingRecord.TransactionId, out var lastRequestTermNumber))
            {
                start = lastRequestTermNumber.Value + 1;
            }

            var end = Math.Min(votingRecord.GetExpireTermNumber(Api.GetBlockchainAge()),
                Api.GetCurrentTermNumber() - 1);

            for (var i = start; i <= end; i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    if (_dividendsMap.TryGet(i.ToUInt64Value(), out var totalDividends))
                    {
                        Console.WriteLine($"Getting dividends of {votingRecord.TransactionId.ToHex()}: ");
                        Console.WriteLine($"Total weights of term {i}: {totalWeights}");
                        Console.WriteLine($"Total dividends of term {i}: {totalDividends}");
                        Console.WriteLine($"Weights of this vote: {votingRecord.Weight}");
                        dividends += totalDividends.Value * votingRecord.Weight / totalWeights.Value;
                        Console.WriteLine($"Result: {dividends}");
                    }
                }
            }

            return dividends;
        }

        [View]
        public ulong GetAllAvailableDividends(string publicKey)
        {
            return Api.GetVotingRecords(publicKey)
                .Where(vr => vr.From == publicKey)
                .Aggregate<VotingRecord, ulong>(0,
                    (current, votingRecord) => current + GetAvailableDividends(votingRecord));
        }

        [View]
        public ulong CheckDividends(ulong ticketsAmount, int lockTime, ulong termNumber)
        {
            var currentTermNumber = Api.GetCurrentTermNumber();
            Api.Assert(termNumber <= currentTermNumber, "Cannot check dividends of future term.");
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                if (_dividendsMap.TryGet(termNumber.ToUInt64Value(), out var totalDividends))
                {
                    return VotingRecord.CalculateWeight(ticketsAmount, lockTime) * totalDividends.Value /
                           totalWeights.Value;
                }
            }

            return 0;
        }

        [View]
        public ULongList CheckDividendsOfPreviousTerm()
        {
            var termNumber = Api.GetCurrentTermNumber() - 1;
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
            var owner = votingRecord.From;
            var ownerAddress =
                Address.FromPublicKey(ByteArrayHelpers.FromHexString(owner));

            var start = votingRecord.TermNumber + 1;
            if (_lastRequestDividendsMap.TryGet(votingRecord.TransactionId, out var history))
            {
                start = history.Value + 1;
            }

            var end = Math.Min(votingRecord.GetExpireTermNumber(Api.GetBlockchainAge()),
                Api.GetCurrentTermNumber() - 1);

            var actualTermNumber = start;
            ulong dividendsAmount = 0;
            for (var i = start; i <= end; i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    if (_dividendsMap.TryGet(i.ToUInt64Value(), out var dividends))
                    {
                        dividendsAmount += dividends.Value * votingRecord.Weight / totalWeights.Value;
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

            Api.SendInline(Api.TokenContractAddress, "Transfer", ownerAddress, dividendsAmount);

            Console.WriteLine($"Gonna transfer {dividendsAmount} dividends to {ownerAddress}");

            _lastRequestDividendsMap.SetValue(votingRecord.TransactionId, actualTermNumber.ToUInt64Value());

            return new ActionResult {Success = true};
        }

        public ActionResult AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            if (_dividendsMap.TryGet(termNumber.ToUInt64Value(), out var dividends) &&
                Api.GetFromAddress() != Api.ConsensusContractAddress)
            {
                var finalDividends = dividends.Value + dividendsAmount;
                _dividendsMap.SetValue(termNumber.ToUInt64Value(), finalDividends.ToUInt64Value());
            }
            else
            {
                _dividendsMap.SetValue(termNumber.ToUInt64Value(), dividendsAmount.ToUInt64Value());
            }

            Console.WriteLine($"Dividends of term {termNumber}: {dividendsAmount}");

            return new ActionResult {Success = true};
        }

        public ActionResult AddWeights(ulong weights, ulong termNumber)
        {
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                var finalWeights = totalWeights.Value + weights;
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), finalWeights.ToUInt64Value());
                Console.WriteLine($"Weights of term {termNumber}: {finalWeights}.[Add]");
            }
            else
            {
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), weights.ToUInt64Value());
                Console.WriteLine($"Weights of term {termNumber}: {weights}.[Add]");
            }

            return new ActionResult {Success = true};
        }

        public ActionResult KeepWeights(ulong oldTermNumber)
        {
            if (_totalWeightsMap.TryGet(oldTermNumber.ToUInt64Value(), out var totalWeights))
            {
                Console.WriteLine("[Forwarding weights]");
                AddWeights(oldTermNumber + 1, totalWeights.Value);
            }

            return new ActionResult {Success = true};
        }

        public ActionResult SubWeights(ulong weights, ulong termNumber)
        {
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                var newWeights = totalWeights.Value - weights;
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), newWeights.ToUInt64Value());
                Console.WriteLine($"Weights of term {termNumber}: {totalWeights}.[Sub]");
            }

            return new ActionResult {Success = true};
        }
    }
}