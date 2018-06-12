using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public class DPoS
    {
        private const int MiningTime = 4;

        // ReSharper disable once InconsistentNaming
        public IDataProvider DPoSDataProvider;

        private IDataProvider _blockProducer;
        private IDataProvider _ins;
        private IDataProvider _outs;
        private IDataProvider _signatures;
        private IDataProvider _timeSlots;
        private IDataProvider _roundsCount;
        private IDataProvider _extraBlockProducer;

        private readonly MainChainNode _node;

        public ulong RoundsCount => UInt64Value.Parser.ParseFrom(_roundsCount.GetAsync(Hash.Zero).Result).Value;

        private bool _isChainIdSetted;

        private readonly IWorldStateManager _worldStateManager;

        public DPoS(IWorldStateManager worldStateManager, MainChainNode node)
        {
            _worldStateManager = worldStateManager;
            _node = node;
        }

        public DPoS OfChain(Hash chainId)
        {
            DPoSDataProvider = new AccountDataProvider(chainId, 
                Path.CalculatePointerForAccountZero(chainId), _worldStateManager).GetDataProvider();

            _blockProducer = DPoSDataProvider.GetDataProvider("BlockProducer");
            _extraBlockProducer = DPoSDataProvider.GetDataProvider("ExtraBlockProducer");
            _ins = DPoSDataProvider.GetDataProvider("Ins");
            _outs = DPoSDataProvider.GetDataProvider("Outs");
            _signatures = DPoSDataProvider.GetDataProvider("Signatures");
            _timeSlots = DPoSDataProvider.GetDataProvider("MiningNodes");
            _roundsCount = DPoSDataProvider.GetDataProvider("RoundsCount");
            
            _isChainIdSetted = true;
            
            return this;
        }

        #region Pre-verification

        public bool PreVerification(Hash inValue, Hash outValue)
        {
            return inValue.CalculateHash() == outValue;
        }

        #endregion
        
        #region Rounds count

        public async Task SetRoundsCount()
        {
            if (await ExtraBlockProducerIdentityVerification(_node.Address))
            {
                await _roundsCount.SetAsync(Hash.Zero, RoundsCountAddOne(await GetRoundsCount()).ToByteArray());
            }
        }

        public async Task<UInt64Value> GetRoundsCount()
        {
            var count = UInt64Value.Parser.ParseFrom(await _roundsCount.GetAsync(Hash.Zero));
            return count;
        }
        
        #endregion

        #region Mining nodes

        public async Task<BlockProducer> GetBlockProducer()
        {
            return BlockProducer.Parser.ParseFrom(await _blockProducer.GetAsync(Hash.Zero));
        }

        /// <summary>
        /// If the first place of this round failed to set the extra block producer,
        /// others can help to do this.
        /// </summary>
        /// <returns></returns>
        public async Task CalculateExtraBlockProducer()
        {
            var roundsCount = await GetRoundsCount();
            if (await _extraBlockProducer.GetAsync(roundsCount.CalculateHash()) != null)
            {
                return;
            }
            
            
        }

        #endregion

        #region Time slots

        public async Task<Timestamp> GetTimeSlotOf(string accountAddress)
        {
            var roundsCount = await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);
            return Timestamp.Parser.ParseFrom(await _timeSlots.GetAsync(key));
        }
        
        #endregion

        #region Ins, Outs, Signatures

        public async Task<Hash> GetInValueOf(string accountAddress)
        {
            var roundsCount = RoundsCountMinusOne(await GetRoundsCount());
            return await _ins.GetAsync(CalculateKeyForRoundRelatedData(roundsCount, accountAddress));
        }
        
        public async Task<Hash> GetOutValueOf(string accountAddress)
        {
            var roundsCount = RoundsCountMinusOne(await GetRoundsCount());
            return await _outs.GetAsync(CalculateKeyForRoundRelatedData(roundsCount, accountAddress));
        }
        
        public async Task<Hash> GetSignatureOf(string accountAddress)
        {
            var roundsCount = RoundsCountMinusOne(await GetRoundsCount());
            return await _signatures.GetAsync(CalculateKeyForRoundRelatedData(roundsCount, accountAddress));
        }
        
        public async Task<Hash> CalculateSignatureOf(string accountAddress = null)
        {
            Interlocked.CompareExchange(ref accountAddress, null, Encoding.UTF8.GetString(_node.Address));
            
            // Get signatures of last round.
            var currentRoundCount = await GetRoundsCount();
            var lastRoundCount = new UInt64Value {Value = currentRoundCount.Value - 1};

            var add = Hash.Zero;
            var miningNodes = await GetBlockProducer();
            foreach (var node in miningNodes.Nodes)
            {
                Hash key = CalculateKeyForRoundRelatedData(lastRoundCount, accountAddress);
                Hash lastSignature = await _signatures.GetAsync(key);
                add = add.CalculateHashWith(lastSignature);
            }

            var inValue = (Hash) await _ins.GetAsync(accountAddress.CalculateHash());
            return inValue.CalculateHashWith(add);
        }
        
        #endregion
        
        public async Task<object> AbleToMine(string accountAddress)
        {
            var assignedTimeSlot = await GetTimeSlotOf(accountAddress);
            var timeSlotEnd = assignedTimeSlot.ToDateTime().AddSeconds(MiningTime).ToTimestamp();

            return CompareTimestamp(assignedTimeSlot, GetTimestamp()) 
                   && CompareTimestamp(timeSlotEnd, assignedTimeSlot);
        }
        
        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using DPoS.");
            }
        }

        private bool MiningNodeIdentityVerification(IEnumerable<byte> address)
        {
            throw new NotImplementedException();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private async Task<bool> ExtraBlockProducerIdentityVerification(IEnumerable<byte> address)
        {
            var roundsCount = await GetRoundsCount();
            var extraBlockProducer = await _extraBlockProducer.GetAsync(roundsCount.CalculateHash());
            return extraBlockProducer.SequenceEqual(address);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
        }
        
        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestamp(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.Now.AddMinutes(offset));
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() > ts2.ToDateTime();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(ulong roundsCount, string blockProducer)
        {
            return new Hash(new UInt64Value {Value = roundsCount}.CalculateHash()).CalculateHashWith(blockProducer);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(IMessage roundsCount, string blockProducer)
        {
            return new Hash(roundsCount.CalculateHash()).CalculateHashWith(blockProducer);
        }
    }
}