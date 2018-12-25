using System;
using System.Collections.Generic;
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

        private const ulong StandardTicketsAmount = 10_000;
        private const int StandardLockTime = 90;

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
            var start = votingRecord.TermNumber;
            if (_lastRequestDividendsMap.TryGet(votingRecord.TransactionId, out var history))
            {
                start = history.Value + 1;
            }
            
            for (var i = start; i <= Api.GetCurrentTermNumber(); i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    if (_dividendsMap.TryGet(i.ToUInt64Value(), out var totalDividends))
                    {
                        dividends += totalDividends.Value * votingRecord.Weight / totalWeights.Value;
                    }
                }
            }

            return dividends;
        }

        [View]
        public ulong GetAvailableDividendsByVotingInformation(Hash transactionId, ulong termNumber, ulong weight)
        {
            ulong dividends = 0;
            var start = termNumber;
            if (_lastRequestDividendsMap.TryGet(transactionId, out var history))
            {
                start = history.Value + 1;
            }
            
            for (var i = start; i <= Api.GetCurrentTermNumber(); i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    if (_dividendsMap.TryGet(i.ToUInt64Value(), out var totalDividends))
                    {
                        dividends += totalDividends.Value * weight / totalWeights.Value;
                    }
                }
            }

            return dividends;
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
        public ulong CheckDividendsOfPreviousTerm(ulong ticketsAmount, int lockTime)
        {
            var currentTermNumber = Api.GetCurrentTermNumber();
            return CheckDividends(ticketsAmount, lockTime, currentTermNumber);
        }

        [View]
        public ulong CheckStandardDividends(ulong termNumber)
        {
            return CheckDividends(StandardTicketsAmount, StandardLockTime, termNumber);
        }

        [View]
        public ulong CheckStandardDividendsOfPreviousTerm(string empty)
        {
            return CheckDividendsOfPreviousTerm(StandardTicketsAmount, StandardLockTime);
        }

        public void TransferDividends(VotingRecord votingRecord, ulong maxTermNumber)
        {
            var owner = votingRecord.From;
            var ownerAddress =
                Address.FromPublicKey(ByteArrayHelpers.FromHexString(owner));
            var start = votingRecord.TermNumber;
            if (_lastRequestDividendsMap.TryGet(votingRecord.TransactionId, out var history))
            {
                start = history.Value + 1;
            }

            var actualTermNumber = start;
            for (var i = start; i <= maxTermNumber; i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    if (_dividendsMap.TryGet(i.ToUInt64Value(), out var dividends))
                    {
                        actualTermNumber = i;
                        var dividendsAmount = dividends.Value * votingRecord.Weight / totalWeights.Value;
                        Console.WriteLine($"Transferred {dividendsAmount} dividends to {owner}");
                        Api.SendInline(Api.TokenContractAddress, "Transfer", ownerAddress, dividendsAmount);
                    }
                }
            }

            _lastRequestDividendsMap.SetValue(votingRecord.TransactionId, actualTermNumber.ToUInt64Value());
        }

        public void AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            Console.WriteLine($"Allowed {dividendsAmount} dividends to term {termNumber}");
            _dividendsMap.SetValue(termNumber.ToUInt64Value(), dividendsAmount.ToUInt64Value());
        }

        public void AddWeights(ulong weights, ulong termNumber)
        {
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                var newWeights = totalWeights.Value + weights;
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), newWeights.ToUInt64Value());
            }
            else
            {
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), weights.ToUInt64Value());
            }

            Console.WriteLine($"Added {weights} weights to {termNumber} term.");
        }

        public void SubWeights(ulong weights, ulong termNumber)
        {
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                var newWeights = totalWeights.Value - weights;
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), newWeights.ToUInt64Value());
            }
        }
    }
}