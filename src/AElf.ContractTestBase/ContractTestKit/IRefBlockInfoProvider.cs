using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.ContractTestBase.ContractTestKit
{
    public sealed class RefBlockInfo
    {
        public long Height { get; }
        public ByteString Prefix { get; }

        public RefBlockInfo(long height, ByteString prefix)
        {
            Height = height;
            Prefix = prefix;
        }
    }

    public interface IRefBlockInfoProvider
    {
        RefBlockInfo GetRefBlockInfo();
    }

    public class RefBlockInfoProvider : IRefBlockInfoProvider, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;

        public RefBlockInfoProvider(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        public RefBlockInfo GetRefBlockInfo()
        {
            var block = AsyncHelper.RunSync(() => _blockchainService.GetBestChainLastBlockHeaderAsync());
            var height = block.Height;
            var prefix = BlockHelper.GetRefBlockPrefix(block.GetHash());
            return new RefBlockInfo(height, prefix);
        }
    }
}