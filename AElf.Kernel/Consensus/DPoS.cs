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

        public UInt64Value RoundsCount => UInt64Value.Parser.ParseFrom(_roundsCount.GetAsync(Hash.Zero).Result);

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
        // ReSharper disable once InconsistentNaming
        public async Task<ITransaction> GenerateEBPCalculationTransaction()
        {
            var roundsCount = await GetRoundsCount();
            if (await _extraBlockProducer.GetAsync(roundsCount.CalculateHash()) != null)
            {
                return null;
            }

            return new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = ""
            };
        }

        #endregion

        #region Time slots

        public async Task<Timestamp> GetTimeSlotOf(byte[] accountAddress)
        {
            var roundsCount = await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);
            return Timestamp.Parser.ParseFrom(await _timeSlots.GetAsync(key));
        }
        
        #endregion

        #region Ins, Outs, Signatures

        public async Task<Hash> GetInValueOf(byte[] accountAddress)
        {
            return await _ins.GetAsync(CalculateKeyForRoundRelatedData(RoundsCount, accountAddress));
        }
        
        public async Task<Hash> GetOutValueOf(byte[] accountAddress)
        {
            return await _outs.GetAsync(CalculateKeyForRoundRelatedData(RoundsCount, accountAddress));
        }
        
        public async Task<Hash> GetSignatureOf(byte[] accountAddress)
        {
            return await _signatures.GetAsync(CalculateKeyForRoundRelatedData(RoundsCount, accountAddress));
        }
        
        public async Task<Hash> CalculateSignatureOf(byte[] accountAddress = null)
        {
            Interlocked.CompareExchange(ref accountAddress, null, _node.Address);
            
            // Get signatures of last round.
            var currentRoundCount = await GetRoundsCount();

            var add = Hash.Zero;
            var blockProducer = await GetBlockProducer();
            foreach (var node in blockProducer.Nodes)
            {
                var key = CalculateKeyForRoundRelatedData(RoundsCountMinusOne(RoundsCount), Encoding.UTF8.GetBytes(node));
                Hash lastSignature = await _signatures.GetAsync(key);
                add = add.CalculateHashWith(lastSignature);
            }

            var inValue = (Hash) await _ins.GetAsync(accountAddress.CalculateHash());
            return inValue.CalculateHashWith(add);
        }
        
        #endregion
        
        public async Task<bool> AbleToMine(byte[] accountAddress)
        {
            var assignedTimeSlot = await GetTimeSlotOf(accountAddress);
            var timeSlotEnd = assignedTimeSlot.ToDateTime().AddSeconds(MiningTime).ToTimestamp();

            return CompareTimestamp(assignedTimeSlot, GetTimestamp()) 
                   && CompareTimestamp(timeSlotEnd, assignedTimeSlot);
        }

        public async Task<bool> TimeToGenerateExtraBlock(byte[] accountAddress)
        {
            if (await ExtraBlockProducerIdentityVerification(accountAddress))
            {
            }
            throw new NotImplementedException();

        }
        
        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using DPoS.");
            }
        }

        private async Task<bool> BlockProducerIdentityVerification(IEnumerable<byte> address)
        {
            var blockProducer = await GetBlockProducer();
            // todo : double-check
            return blockProducer.Nodes.Contains(address.ToString());
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private async Task<bool> ExtraBlockProducerIdentityVerification(IEnumerable<byte> address)
        {
            var extraBlockProducer = await _extraBlockProducer.GetAsync(RoundsCount.CalculateHash());
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
        private Hash CalculateKeyForRoundRelatedData(ulong roundsCount, byte[] blockProducer)
        {
            return new Hash(new UInt64Value {Value = roundsCount}.CalculateHash()).CalculateHashWith(blockProducer.ToString());
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(IMessage roundsCount, byte[] blockProducer)
        {
            return new Hash(roundsCount.CalculateHash()).CalculateHashWith(blockProducer.ToString());
        }
    }
}