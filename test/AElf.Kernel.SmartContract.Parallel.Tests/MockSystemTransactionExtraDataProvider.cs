using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockSystemTransactionExtraDataProvider : ISystemTransactionExtraDataProvider
    {
        private const string BlockHeaderExtraDataKey = "SystemTransactionCount";

        public bool TryGetSystemTransactionCount(BlockHeader blockHeader,out int count)
        {
            count = 0;
            
            var byteString = blockHeader.ExtraData.TryGetValue(BlockHeaderExtraDataKey, out var extraData)
                ? extraData
                : null;
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