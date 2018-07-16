using System;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus
{
    public class DPoSCheck
    {
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly ECKeyPair _keyPair;
        private readonly IDataProvider _dataProvider;
        private readonly Hash _chainId;
        private readonly BlockProducer _blockProducer;
        private readonly Hash _contractAddressHash;

        private UInt64Value RoundsCount
        {
            get
            {
                var bytes = _dataProvider.GetAsync("RoundsCount".CalculateHash()).Result;
                return UInt64Value.Parser.ParseFrom(bytes);
            }
        }

        public DPoSCheck(IWorldStateDictator worldStateDictator, ECKeyPair keyPair, Hash chainId, BlockProducer blockProducer, Hash contractAddressHash)
        {
            _worldStateDictator = worldStateDictator.SetChainId(chainId);
            _keyPair = keyPair;
            _chainId = chainId;
            _blockProducer = blockProducer;
            _contractAddressHash = contractAddressHash;

            _dataProvider = _worldStateDictator.GetAccountDataProvider(contractAddressHash).Result.GetDataProvider();

        }

        public async Task<bool> AbleToMine()
        {
            var accountHash = _keyPair.GetAddress();
            var accountAddress = AddressHashToString(accountHash);
            var now = GetTimestampOfUtcNow();

            if (!_blockProducer.Nodes.Contains(accountAddress))
            {
                return false;
            }
            
            var assignedTimeSlot = await GetTimeSlot(accountAddress);
            var timeSlotEnd = GetTimestamp(assignedTimeSlot, Globals.MiningTime);
            
            return CompareTimestamp(now, assignedTimeSlot) && CompareTimestamp(timeSlotEnd, now);
        }


        private async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            var bytes = await _dataProvider.GetDataProvider("DPoSInfo").GetAsync(RoundsCount.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(string accountAddress, UInt64Value roundsCount)
        {
            var bytes = await _dataProvider.GetDataProvider("DPoSInfo").GetAsync(roundsCount.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().ToHex().Remove(0, 2);
        }
        
        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
        }

        private Timestamp GetTimestamp(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
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