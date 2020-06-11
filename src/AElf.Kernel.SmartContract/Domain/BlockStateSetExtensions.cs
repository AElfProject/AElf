using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.Domain
{
    public static class BlockStateSetExtensions
    {
        public static TieredStateCache ToTieredStateCache(this BlockStateSet blockStateSet)
        {
            var groupStateCache = blockStateSet == null
                ? new TieredStateCache()
                : new TieredStateCache(
                    new StateCacheFromPartialBlockStateSet(blockStateSet));

            return groupStateCache;
        }
    }
}