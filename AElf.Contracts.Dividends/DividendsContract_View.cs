using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Dividends
{
    public partial class DividendsContract
    {
        /// <summary>
        /// Get dividends of a specific term.
        /// </summary>
        /// <param name="termNumber"></param>
        /// <returns></returns>
        [View]
        public ulong GetTermDividends(ulong termNumber)
        {
            return State.DividendsMap[termNumber];
        }

        /// <summary>
        /// Get total weights of a specific term.
        /// </summary>
        /// <param name="termNumber"></param>
        /// <returns></returns>
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
            ulong dividends = 0;

            var start = votingRecord.TermNumber + 1;
            var lastRequestTermNumber = State.LastRequestDividendsMap[votingRecord.TransactionId];
            if (lastRequestTermNumber > 0)
            {
                start = lastRequestTermNumber + 1;
            }

            var end = Math.Min(GetExpireTermNumber(votingRecord, State.ConsensusContract.GetBlockchainAge()),
                State.ConsensusContract.GetCurrentTermNumber() - 1);

            for (var i = start; i <= end; i++)
            {
                var totalWeights = State.TotalWeightsMap[i];
                if (totalWeights > 0)
                {
                    var totalDividends = State.DividendsMap[i];
                    if (totalDividends > 0)
                    {
                        Context.LogDebug(()=>$"Getting dividends of {votingRecord.TransactionId.ToHex()}: ");
                        Context.LogDebug(()=>$"Total weights of term {i}: {totalWeights}");
                        Context.LogDebug(()=>$"Total dividends of term {i}: {totalDividends}");
                        Context.LogDebug(()=>$"Weights of this vote: {votingRecord.Weight}");
                        dividends += totalDividends * votingRecord.Weight / totalWeights;
                        Context.LogDebug(()=>$"Result: {dividends}");
                    }
                }
            }

            return dividends;
        }
        
        public ulong GetExpireTermNumber(VotingRecord votingRecord, ulong currentAge)
        {
            return votingRecord.TermNumber + GetDurationDays(votingRecord, currentAge) / 7;
        }
        
        public ulong GetDurationDays(VotingRecord votingRecord, ulong currentAge)
        {
            var days = currentAge - votingRecord.VoteAge + 1;
            ulong totalLockDays = 0;
            foreach (var d in votingRecord.LockDaysList)
            {
                totalLockDays += (ulong) d;
            }

            return Math.Min(days, totalLockDays);
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
    }
}