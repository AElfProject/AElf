using System;
using System.Threading.Tasks;
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

            _isChainIdSetted = true;
            
            return this;
        }

        public async Task SetRoundsCount()
        {
            if (ProofOfIdentityOfExtraBlockProducer())
            {
                await _roundsCount.SetAsync(Hash.Zero, RoundsCountAddOne(await GetRoundsCount()).ToByteArray());
            }
        }

        public async Task<UInt64Value> GetRoundsCount()
        {
            var count = UInt64Value.Parser.ParseFrom(await _roundsCount.GetAsync(Hash.Zero));
            return count;
        }
        
        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using DPoS.");
            }
        }

        private bool ProofOfIdentityOfMiningNode()
        {
            throw new NotImplementedException();
        }
        
        private bool ProofOfIdentityOfExtraBlockProducer()
        {
            throw new NotImplementedException();
        }

        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
    }
}