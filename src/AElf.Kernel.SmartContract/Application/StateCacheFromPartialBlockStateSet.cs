using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public class StateCacheFromPartialBlockStateSet : IStateCache
    {
        private readonly BlockStateSet _blockStateSet;

        public StateCacheFromPartialBlockStateSet(BlockStateSet blockStateSet)
        {
            _blockStateSet = blockStateSet;
        }

        public bool TryGetValue(ScopedStatePath key, out byte[] value)
        {
            var stateKey = key.ToStateKey();
            var found = _blockStateSet.Deletes.Contains(stateKey);
            if (found)
            {
                value = null;
                return true;
            }
            found = _blockStateSet.Changes.TryGetValue(stateKey, out var bs);
            value = found ? bs.ToByteArray() : null;
            return found;
        }

        public byte[] this[ScopedStatePath key]
        {
            get => TryGetValue(key, out var value) ? value : null;
            set
            {
                //do nothing
            }
        }
    }
}