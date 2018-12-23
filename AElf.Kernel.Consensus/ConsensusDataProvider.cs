using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public class ConsensusDataProvider
    {
        private readonly IStateStore _stateStore;

        public ILogger<ConsensusDataProvider> Logger { get; set; }

        public ConsensusDataProvider(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        
        //TODO: configuration need be changed.
        public Hash ChainId => Hash.LoadBase58(ChainConfig.Instance.ChainId);

        public Address ContractAddress => ContractHelpers.GetConsensusContractAddress(
            Hash.LoadBase58(ChainConfig.Instance.ChainId));

        private DataProvider DataProvider
        {
            get
            {
                var dp = DataProvider.GetRootDataProvider(ChainId, ContractAddress);
                dp.StateStore = _stateStore;
                return dp;
            }
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        private async Task<byte[]> GetBytes<T>(Hash keyHash, string resourceStr = "") where T : IMessage, new()
        {
            return await (resourceStr != ""
                ? DataProvider.GetChild(resourceStr).GetAsync<T>(keyHash)
                : DataProvider.GetAsync<T>(keyHash));
        }

        public async Task<Miners> GetMiners()
        {
            try
            {
                var miners =
                    Miners.Parser.ParseFrom(
                        await GetBytes<Miners>(Hash.FromString(GlobalConfig.AElfDPoSMinersString)));
                return miners;
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "Failed to get miners list.");
                return new Miners();
            }
        }

        public async Task<ulong> GetCurrentRoundNumber()
        {
            try
            {
                var number = UInt64Value.Parser.ParseFrom(
                    await GetBytes<UInt64Value>(Hash.FromString(GlobalConfig.AElfDPoSCurrentRoundNumber)));
                return number.Value;
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "Failed to current round number.");
                return 0;
            }
        }

        public async Task<Round> GetCurrentRoundInfo()
        {
            var currentRoundNumber = await GetCurrentRoundNumber();
            try
            {
                var bytes = await GetBytes<Round>(Hash.FromMessage(new UInt64Value {Value = currentRoundNumber}),
                    GlobalConfig.AElfDPoSRoundsMapString);
                var round = Round.Parser.ParseFrom(bytes);
                return round;
            }
            catch (Exception e)
            {
                Logger.LogError(e,
                    $"Failed to get Round information of provided round number. - {currentRoundNumber}\n");
                return null;
            }
        }

        public async Task<MinerInRound> GetMinerInfo(string publicKey = null)
        {
            if (publicKey == null)
            {
                publicKey = NodeConfig.Instance.ECKeyPair.PublicKey.ToHex();
            }

            var round = await GetCurrentRoundInfo();
            return round.RealTimeMinersInfo[publicKey];
        }

        public async Task<Timestamp> GetExpectMiningTime(string publicKey = null)
        {
            if (publicKey == null)
            {
                publicKey = NodeConfig.Instance.ECKeyPair.PublicKey.ToHex();
            }

            var info = await GetMinerInfo(publicKey);
            return info.ExpectedMiningTime;
        }

        public async Task<double> GetDistanceToTimeSlot(string publicKey = null)
        {
            if (publicKey == null)
            {
                publicKey = NodeConfig.Instance.ECKeyPair.PublicKey.ToHex();
            }

            var timeSlot = await GetExpectMiningTime(publicKey);
            var distance = timeSlot - DateTime.UtcNow.ToTimestamp();
            return distance.ToTimeSpan().TotalMilliseconds;
        }

        public async Task<double> GetDistanceToTimeSlotEnd(string publicKey = null)
        {
            var distance = (double) ConsensusConfig.Instance.DPoSMiningInterval;
            var currentRoundNumber = await GetCurrentRoundNumber();
            if (currentRoundNumber != 0)
            {
                var info = await GetMinerInfo(publicKey);

                var now = DateTime.UtcNow.ToTimestamp();
                distance += (info.ExpectedMiningTime - now).ToTimeSpan().TotalMilliseconds;
                if (info.IsExtraBlockProducer && distance < 0)
                {
                    distance += (GlobalConfig.BlockProducerNumber - info.Order + 2) *
                                ConsensusConfig.Instance.DPoSMiningInterval;
                }
            }

            // Todo the time slot of dpos is not exact
            return (distance < 1000 || distance > (double) ConsensusConfig.Instance.DPoSMiningInterval)
                ? ConsensusConfig.Instance.DPoSMiningInterval
                : distance;
        }
    }
}