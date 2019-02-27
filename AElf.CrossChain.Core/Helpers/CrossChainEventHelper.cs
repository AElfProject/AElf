using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public static class CrossChainEventHelper
    {
        public static object[] ExtractCrossChainBlockDataFromEvent(LogEvent logEvent)
        {
            return ParamsPacker.Unpack(logEvent.Data.ToByteArray(), new[] {typeof(Hash), typeof(CrossChainBlockData)});
        }

        public static bool TryGetLogEventInBlock(IBlock block, out LogEvent logEvent)
        {
            logEvent = new LogEvent
            {
                Address = ContractHelpers.GetGenesisBasicContractAddress(block.Header.ChainId),
                Topics =
                {
                    ByteString.CopyFrom(CrossChainConsts.CrossChainIndexingEvent.CalculateHash())
                }
            };
            return logEvent.GetBloom().IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }
    }
}