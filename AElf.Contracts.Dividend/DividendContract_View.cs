using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Dividend
{
    public partial class DividendContract
    {
        /// <summary>
        /// Get dividends of a specific term.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetTermDividends(SInt64Value input)
        {
            return new SInt64Value {Value = State.DividendsMap[input.Value]};
        }

        /// <summary>
        /// Get total weights of a specific term.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetTermTotalWeights(SInt64Value input)
        {
            return new SInt64Value {Value = State.TotalWeightsMap[input.Value]};
        }

        public override SInt64Value GetLatestRequestDividendsTermNumber(VotingRecord input)
        {
            var termNumber = State.LastRequestedDividendsMap[input.TransactionId];
            if (termNumber != 0)
            {
                return new SInt64Value
                {
                    Value = termNumber
                };
            }
            return new SInt64Value {Value = input.TermNumber};
        }

        public override SInt64Value GetAvailableDividends(VotingRecord input)
        {
            var votingRecord = input;
            long dividends = 0;

            var start = votingRecord.TermNumber + 1;
            var lastRequestTermNumber = State.LastRequestedDividendsMap[votingRecord.TransactionId];
            if (lastRequestTermNumber > 0)
            {
                start = lastRequestTermNumber + 1;
            }

            var voteInfo = new VoteInfo
            {
                Record = votingRecord,
                Age = State.ConsensusContract.GetBlockchainAge.Call(new Empty()).Value
            };
            var end = Math.Min(GetExpireTermNumber(voteInfo).Value,
                State.ConsensusContract.GetCurrentTermNumber.Call(new Empty()).Value - 1);

            for (var i = start; i <= end; i++)
            {
                var totalWeights = State.TotalWeightsMap[i];
                if (totalWeights > 0)
                {
                    var totalDividends = State.DividendsMap[i];
                    if (totalDividends > 0)
                    {
                        dividends += totalDividends * votingRecord.Weight / totalWeights;
                    }
                }
            }

            return new SInt64Value {Value = dividends};
        }

        public override SInt64Value GetExpireTermNumber(VoteInfo input)
        {
            var termNumber = input.Record.TermNumber + GetDurationDays(input).Value / ConsensusDPoSConsts.DaysEachTerm;
            return new SInt64Value {Value = termNumber};
        }

        public override SInt64Value GetDurationDays(VoteInfo input)
        {
            var votingRecord = input.Record;
            var currentAge = input.Age;
            var days = currentAge - votingRecord.VoteAge + 1;
            var totalLockDays = 0L;
            foreach (var d in votingRecord.LockDaysList)
            {
                totalLockDays += d;
            }

            return new SInt64Value {Value = Math.Min(days, totalLockDays)};
        }

        public override SInt64Value GetAllAvailableDividends(PublicKey input)
        {
            var ticketsInformation = State.ConsensusContract.GetTicketsInformation.Call(
                new PublicKey
                {
                    Hex = input.Hex
                });
            if (ticketsInformation == null || !ticketsInformation.VotingRecords.Any())
            {
                return new SInt64Value();
            }

            var dividends = ticketsInformation.VotingRecords
                .Where(vr => vr.From == input.Hex)
                .Aggregate<VotingRecord, long>(0,
                    (current, votingRecord) => current + GetAvailableDividends(votingRecord).Value);
            return new SInt64Value {Value = dividends};
        }

        public override SInt64Value CheckDividends(CheckDividendsInput input)
        {
            var termNumber = input.TermNumber;
            var ticketsAmount = input.TicketsAmount;
            var lockTime = input.LockTime;
            var currentTermNumber = State.ConsensusContract.GetCurrentTermNumber.Call(new Empty()).Value;
            if (termNumber >= currentTermNumber)
            {
                return new SInt64Value();
            }
            
            var totalWeights = State.TotalWeightsMap[termNumber];
            if (totalWeights > 0)
            {
                var totalDividends = State.DividendsMap[termNumber];
                if (totalDividends > 0)
                {
                    var weights = VotingRecord.CalculateWeight(ticketsAmount, lockTime) * totalDividends /
                           totalWeights;
                    return new SInt64Value {Value = weights};
                }
            }

            return new SInt64Value();
        }

        public override LongList CheckDividendsOfPreviousTerm(Empty input)
        {
            var termNumber = State.ConsensusContract.GetCurrentTermNumber.Call(new Empty()).Value - 1;
            var result = new LongList();

            if (termNumber < 1)
            {
                return new LongList {Values = {0}};
            }

            const long ticketsAmount = 10_000;
            var lockTimes = new List<int> {30, 180, 365, 730, 1095};
            foreach (var lockTime in lockTimes)
            {
                result.Values.Add(CheckDividends(new CheckDividendsInput
                {
                    TermNumber = termNumber,
                    TicketsAmount = ticketsAmount,
                    LockTime = lockTime
                }).Value);
            }

            return result;
        }

        public override FriendlyString CheckDividendsOfPreviousTermToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = CheckDividendsOfPreviousTerm(input).ToString()};
        }
    }
}