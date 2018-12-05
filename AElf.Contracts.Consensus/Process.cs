using System;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using NLog.Fluent;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class Process
    {
        private int Interval
        {
            get
            {
                var interval = _collection.MiningIntervalField.GetValue();
                return interval == 0 ? ConsensusConfig.Instance.DPoSMiningInterval : interval;
            }
        }

        private ulong CurrentRoundNumber => _collection.CurrentRoundNumberField.GetValue();

        private int LogLevel { get; set; }

        private readonly AElfDPoSFieldMapCollection _collection;

        public Process(AElfDPoSFieldMapCollection collection)
        {
            _collection = collection;
        }

        public void Initialize(Miners miners, AElfDPoSInformation dpoSInformation, int miningInterval, int logLevel)
        {
            InitializeMiners(miners);

            LogLevel = logLevel;
        }

        public void Update(Round currentRoundInfo, Round nextRoundInfo, string nextExtraBlockProducer, long roundId)
        {

        }

        public void PublishOutValue(ulong roundNumber, Hash outValue, Hash signature, long roundId)
        {

        }

        public void PublishInValue(ulong roundNumber, Hash inValue, long roundId)
        {

        }

        private void InitializeMiners(Miners miners)
        {
            var candidates = new Candidates();
            foreach (var pubKey in miners.Producers)
            {
                ConsoleWriteLine(nameof(Initialize), $"Set miner {pubKey.ToByteArray().ToHex()} to state store.");

                candidates.PubKeys.Add(pubKey);

                // This should only happen on main chain. 
                var bv = new BytesValue
                {
                    Value = pubKey
                };
                if (!_collection.BalanceMap.TryGet(bv, out var tickets))
                {
                    // Miners in the white list
                    tickets = new Tickets {TotalTickets = GlobalConfig.LockTokenForElection};
                    _collection.BalanceMap.SetValue(bv, tickets);
                }
            }

            await _collection.CandidatesField.SetAsync(candidates);
        }
        
        private DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }

        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        private void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            // Debug level: 6=Off, 5=Fatal 4=Error, 3=Warn, 2=Info, 1=Debug, 0=Trace
            if (LogLevel == 6)
                return;

            Console.WriteLine($"[{GetLocalTime():yyyy-MM-dd HH:mm:ss.fff} - AElfDPoS]{prefix} - {log}.");
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Return true if ts1 >= ts2
        /// </summary>
        /// <param name="ts1"></param>
        /// <param name="ts2"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() >= ts2.ToDateTime();
        }
    }
}