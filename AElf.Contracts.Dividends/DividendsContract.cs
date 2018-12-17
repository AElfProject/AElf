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
            return _lastRequestDividendsMap.TryGet(GetHashOfVotingRecord(votingRecord), out var termNumber)
                ? termNumber.Value
                : votingRecord.TermNumber;
        }

        public void TransferDividends(VotingRecord votingRecord, ulong maxTermNumber)
        {
            var owner = votingRecord.From;
            var ownerAddress =
                Address.FromPublicKey(Api.ChainId.DumpByteArray(), ByteArrayHelpers.FromHexString(owner));
            var start = votingRecord.TermNumber;
            if (_lastRequestDividendsMap.TryGet(GetHashOfVotingRecord(votingRecord), out var history))
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

            _lastRequestDividendsMap.SetValue(GetHashOfVotingRecord(votingRecord), actualTermNumber.ToUInt64Value());
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

        private Hash GetHashOfVotingRecord(VotingRecord votingRecord)
        {
            return Hash.FromMessage(votingRecord.ToSimpleRecord());
        }
    }
}