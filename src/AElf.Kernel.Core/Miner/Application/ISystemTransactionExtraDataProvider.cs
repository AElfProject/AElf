using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionExtraDataProvider
    {
        bool TryGetSystemTransactionCount(BlockHeader blockHeader,out int count);

        void SetSystemTransactionCount(int count, BlockHeader blockHeader);
    }
    
    public class SystemTransactionExtraDataProvider : ISystemTransactionExtraDataProvider, ISingletonDependency
    {
        private const string BlockHeaderExtraDataKey = "SystemTransactionCount";
        private readonly IBlockExtraDataService _blockExtraDataService;

        public SystemTransactionExtraDataProvider(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public bool TryGetSystemTransactionCount(BlockHeader blockHeader,out int count)
        {
            count = 0;
            var byteString = _blockExtraDataService.GetExtraDataFromBlockHeader(BlockHeaderExtraDataKey, blockHeader);
            if (byteString == null) return false;
            count = Int32Value.Parser.ParseFrom(byteString).Value;
            return true;
        }

        public void SetSystemTransactionCount(int count, BlockHeader blockHeader)
        {
            blockHeader.ExtraData.Add(BlockHeaderExtraDataKey, new Int32Value {Value = count}.ToByteString());
        }
    }
}