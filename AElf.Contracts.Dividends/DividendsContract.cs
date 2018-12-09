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
        private readonly Map<UInt64Value, UInt64Value> _dividendsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.DividendsMapString);

        private readonly Map<UInt64Value, UInt64Value> _totalWeightsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.TotalWeightsMapString);
        
        private readonly PbField<TermRounds> _termRoundsField =
            new PbField<TermRounds>(GlobalConfig.TermRoundsFieldString);
        
        public void GetDividends(VotingRecord votingRecord)
        {
            var owner = Api.GetFromAddress();
            var roundNumber = _termRoundsField.GetValue().GetLatestRoundNumber();
            if (_totalWeightsMap.TryGet(roundNumber.ToUInt64Value(), out var totalWeights))
            {
                Api.Call(Api.TokenContractAddress, "Transfer",
                    ParamsPacker.Pack(new List<object> {owner, votingRecord.Weight / totalWeights.Value}));
            }
        }

        public void AddDividends(ulong roundNumber, ulong amount, ulong totalWeights)
        {
            _dividendsMap.SetValue(roundNumber.ToUInt64Value(), amount.ToUInt64Value());
            _totalWeightsMap.SetValue(roundNumber.ToUInt64Value(), totalWeights.ToUInt64Value());
            var termRounds = _termRoundsField.GetValue();
            termRounds?.RoundNumbers.Add(roundNumber);
            _termRoundsField.SetValue(termRounds);
        }
    }
}