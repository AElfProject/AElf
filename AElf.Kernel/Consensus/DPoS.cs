using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public class DPoS
    {
        // ReSharper disable once InconsistentNaming
        public IDataProvider DPoSDataProvider;

        private IDataProvider _miningNodes;
        private IDataProvider _ins;
        private IDataProvider _outs;
        private IDataProvider _signatures;
        private IDataProvider _timeSlots;
        private IDataProvider _roundsCount;
        private IDataProvider _extraBlockProducer;

        public ulong RoundsCount => UInt64Value.Parser.ParseFrom(_roundsCount.GetAsync(Hash.Zero).Result).Value;

        private bool _isChainIdSetted;

        private readonly IWorldStateManager _worldStateManager;

        public DPoS(IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
        }

        public DPoS OfChain(Hash chainId)
        {
            DPoSDataProvider = new AccountDataProvider(chainId, 
                Path.CalculatePointerForAccountZero(chainId), _worldStateManager).GetDataProvider();

            _miningNodes = DPoSDataProvider.GetDataProvider("MiningNodes");
            _ins = DPoSDataProvider.GetDataProvider("Ins");
            _outs = DPoSDataProvider.GetDataProvider("Outs");
            _signatures = DPoSDataProvider.GetDataProvider("Signatures");
            _timeSlots = DPoSDataProvider.GetDataProvider("MiningNodes");
            _roundsCount = DPoSDataProvider.GetDataProvider("RoundsCount");
            _extraBlockProducer = DPoSDataProvider.GetDataProvider("ExtraBlockProducer");
            
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
            if (ExtraBlockProducerIdentityVerification())
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

        public async Task<MiningNodes> GetMiningNodes()
        {
            return MiningNodes.Parser.ParseFrom(await _miningNodes.GetAsync(Hash.Zero));
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

        public async Task<Timestamp> GetTimeSlotOf(Hash accountHash)
        {
            var roundsCount = await GetRoundsCount();
            var key = accountHash.CalculateHashWith((Hash) roundsCount.CalculateHash());
            return Timestamp.Parser.ParseFrom(await _timeSlots.GetAsync(key));
        }
        
        #endregion

        #region Ins, Outs, Signatures

        public async Task<Hash> GetInValueOf(Hash accountHash)
        {
            
        }
        
        public async Task<Hash> GetOutValueOf(Hash accountHash)
        {
            
        }
        
        public async Task<Hash> GetSignatureOf(Hash accountHash)
        {
            
        }
        
        public async Task<Hash> CalculateSignatureOf(Hash accountHash)
        {
            // Get signatures of last round.
            var currentRoundCount = await GetRoundsCount();
            var lastRoundCount = new UInt64Value {Value = currentRoundCount.Value - 1};
            Hash roundCountHash = lastRoundCount.CalculateHash();

            var add = Hash.Zero;
            var miningNodes = await GetMiningNodes();
            foreach (var node in miningNodes.Nodes)
            {
                Hash key = node.CalculateHashWith(roundCountHash);
                Hash lastSignature = await _signatures.GetAsync(key);
                add = add.CalculateHashWith(lastSignature);
            }

            var inValue = (Hash) await _ins.GetAsync(accountHash);
            return inValue.CalculateHashWith(add);
        }
        
        #endregion
        
        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using DPoS.");
            }
        }

        private bool MiningNodeIdentityVerification()
        {
            throw new NotImplementedException();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool ExtraBlockProducerIdentityVerification(Hash accountHash)
        {
             
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
    }
}