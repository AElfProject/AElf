using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Dividend
{
    public partial class DividendsContract
    {
        /// <summary>
        /// Get dividends of a specific term.
        /// </summary>
        /// <param name="termNumber"></param>
        /// <returns></returns>
        [View]
        public long GetTermDividends(long termNumber)
        {
            return State.DividendsMap[termNumber];
        }

        /// <summary>
        /// Get total weights of a specific term.
        /// </summary>
        /// <param name="termNumber"></param>
        /// <returns></returns>
        [View]
        public long GetTermTotalWeights(long termNumber)
        {
            return State.TotalWeightsMap[termNumber];
        }

        [View]
        public long GetLatestRequestDividendsTermNumber(VotingRecord votingRecord)
        {
            var termNumber = State.LastRequestedDividendsMap[votingRecord.TransactionId];
            return termNumber != 0
                ? termNumber
                : votingRecord.TermNumber;
        }

        [View]
        public long GetAvailableDividends(VotingRecord votingRecord)
        {
            long dividends = 0;

            var start = votingRecord.TermNumber + 1;
            var lastRequestTermNumber = State.LastRequestedDividendsMap[votingRecord.TransactionId];
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
                        dividends += totalDividends * votingRecord.Weight / totalWeights;
                    }
                }
            }

            return dividends;
        }

        public long GetExpireTermNumber(VotingRecord votingRecord, long currentAge)
        {
            return votingRecord.TermNumber + GetDurationDays(votingRecord, currentAge) / ConsensusDPoSConsts.DaysEachTerm;
        }

        public long GetDurationDays(VotingRecord votingRecord, long currentAge)
        {
            var days = currentAge - votingRecord.VoteAge + 1;
            var totalLockDays = 0L;
            foreach (var d in votingRecord.LockDaysList)
            {
                totalLockDays += (long) d;
            }

            return Math.Min(days, totalLockDays);
        }

        [View]
        public long GetAllAvailableDividends(string publicKey)
        {
            var ticketsInformation = State.ConsensusContract.GetTicketsInfo(publicKey);
            if (ticketsInformation == null || ticketsInformation.VotingRecords.Any())
            {
                return 0;
            }

            return ticketsInformation.VotingRecords
                .Where(vr => vr.From == publicKey)
                .Aggregate<VotingRecord, long>(0,
                    (current, votingRecord) => current + GetAvailableDividends(votingRecord));
        }

        [View]
        public long CheckDividends(long ticketsAmount, int lockTime, long termNumber)
        {
            var currentTermNumber = State.ConsensusContract.GetCurrentTermNumber();
            if (termNumber >= currentTermNumber)
            {
                return 0;
            }
            
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
        public LongList CheckDividendsOfPreviousTerm()
        {
            var termNumber = State.ConsensusContract.GetCurrentTermNumber() - 1;
            var result = new LongList();

            if (termNumber < 1)
            {
                return new LongList {Values = {0}};
            }

            const long ticketsAmount = 10_000;
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