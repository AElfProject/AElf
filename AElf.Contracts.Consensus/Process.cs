using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

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

        private readonly DataCollection _collection;

        public Process(DataCollection collection)
        {
            _collection = collection;
        }

        public void Initialize(Term firstTerm, int logLevel)
        {
            _collection.CurrentRoundNumberField.SetValue(1);

            SetAliases(firstTerm);

            _collection.RoundsMap.SetValue(new UInt64Value {Value = 1}, firstTerm.FirstRound);
            _collection.RoundsMap.SetValue(new UInt64Value {Value = 2}, firstTerm.SecondRound);

            LogLevel = logLevel;
        }

        public void NextTerm()
        {
            
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
        
        #region Vital Steps

        private void SetAliases(Term term)
        {
            var index = 0;
            foreach (var publicKey in term.Miners.PublicKeys)
            {
                if (index >= Config.Aliases.Count) 
                    continue;

                var alias = Config.Aliases[index];
                _collection.AliasesMap.SetValue(new StringValue {Value = publicKey},
                    new StringValue {Value = alias});
                ConsoleWriteLine(nameof(SetAliases), $"Set alias {alias} to {publicKey}");
                index++;
            }
        }
        
        #endregion

        private DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }

        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        private string GetAlias(string publicKey)
        {
            return _collection.AliasesMap.TryGet(new StringValue {Value = publicKey}, out var alias)
                ? alias.Value
                : publicKey;
        }

        /// <summary>
        /// Debug level:
        /// 6 = Off
        /// 5 = Fatal
        /// 4 = Error
        /// 3 = Warn
        /// 2 = Info
        /// 1 = Debug
        /// 0 = Trace
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="log"></param>
        /// <param name="ex"></param>
        private void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            if (LogLevel == 6)
                return;

            Console.WriteLine($"[{GetLocalTime():yyyy-MM-dd HH:mm:ss.fff} - Consensus]{prefix} - {log}.");
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