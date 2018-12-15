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

        private readonly Map<UInt64Value, UInt64Value> _totalWeightsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.TotalWeightsMapString);
        
        private readonly Map<Hash, UInt64Value> _transferMap =
            new Map<Hash, UInt64Value>(GlobalConfig.TransferMapString);
        
        public void TransferDividends(VotingRecord votingRecord, ulong maxTermNumber)
        {
            var owner = Api.GetToAddress();
            var start = votingRecord.TermNumber;
            if (_transferMap.TryGet(Hash.FromMessage(votingRecord.ToSimpleRecord()), out var history))
            {
                start = history.Value + 1;
            }

            var actualTermNumber = start;
            for (var i = start; i <= maxTermNumber; i++)
            {
                if (_totalWeightsMap.TryGet(i.ToUInt64Value(), out var totalWeights))
                {
                    actualTermNumber = i;
                    var minedBlocks = Api.GetTermSnapshot(i).TotalBlocks;
                    var dividendsAmount = minedBlocks * GlobalConfig.ElfTokenPerBlock * votingRecord.Weight /
                                    totalWeights.Value;
                    Console.WriteLine($"Transferred {dividendsAmount} dividends to {owner}");
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object>
                        {
                            owner,
                            dividendsAmount
                        }));
                }
            }

            _transferMap.SetValue(Hash.FromMessage(votingRecord.ToSimpleRecord()), actualTermNumber.ToUInt64Value());
        }

        public void AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            _dividendsMap.SetValue(termNumber.ToUInt64Value(), dividendsAmount.ToUInt64Value());
        }

        public void AddWights(ulong weights, ulong termNumber)
        {
            if (_totalWeightsMap.TryGet(termNumber.ToUInt64Value(), out var totalWeights))
            {
                var newWeights = totalWeights.Value + weights;
                _totalWeightsMap.SetValue(termNumber.ToUInt64Value(), newWeights.ToUInt64Value());
            }
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