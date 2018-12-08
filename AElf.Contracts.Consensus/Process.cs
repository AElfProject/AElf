using System;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

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
            UpdateCurrentRoundNumber();

            SetAliases(firstTerm);

            _collection.RoundsMap.SetValue(((ulong) 1).ToUInt64Value(), firstTerm.FirstRound);
            _collection.RoundsMap.SetValue(((ulong) 2).ToUInt64Value(), firstTerm.SecondRound);

            LogLevel = logLevel;
        }

        public void NextTerm(Term term)
        {
            UpdateCurrentRoundNumber();

            _collection.RoundsMap.SetValue(CurrentRoundNumber.ToUInt64Value(), term.FirstRound);
            _collection.RoundsMap.SetValue((CurrentRoundNumber + 1).ToUInt64Value(), term.SecondRound);
        }

        public void Update(Forwarding forwarding)
        {
            var forwardingCurrentRoundInfo = forwarding.CurrentRoundInfo;
            if (_collection.RoundsMap.TryGet(forwardingCurrentRoundInfo.RoundNumber.ToUInt64Value(), out var currentRoundInfo))
            {
                Api.Assert(forwardingCurrentRoundInfo.RoundId == currentRoundInfo.RoundId, "Round Id not matched.");
            }

            SupplyCurrentRoundInfo(currentRoundInfo, forwardingCurrentRoundInfo);
            
            WindUp();
        }

        public void PublishOutValue(ToPackage toPackage)
        {

        }

        public void PublishInValue(ToBroadcast toBroadcast)
        {

        }
        
        #region Vital Steps

        private void UpdateCurrentRoundNumber()
        {
            var currentRoundNumber = CurrentRoundNumber;
            if (currentRoundNumber == 0)
            {
                _collection.CurrentRoundNumberField.SetValue(1);
            }
            else
            {
                _collection.CurrentRoundNumberField.SetValue(currentRoundNumber + 1);
            }
        }

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

        /// <summary>
        /// Can only supply signature, out value, in value if one missed his time slot.
        /// </summary>
        /// <param name="roundInfo"></param>
        /// <param name="forwardingRoundInfo"></param>
        private void SupplyCurrentRoundInfo(Round roundInfo, Round forwardingRoundInfo)
        {
            
        }

        private void WindUp()
        {
            // Check in and out value, complain if not match.
            
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
                : publicKey.Substring(5);
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