using System.Linq;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestKit
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
            var prefix = ByteString.CopyFrom(block.GetHash().Value.Take(4).ToArray());
            return new RefBlockInfo(height, prefix);
        }
    }
}