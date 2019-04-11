using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
{
    //TODO: Can be removed as it is not used.
    public static class CrossChainEventHelper
    {
//        public static bool TryGetLogEventInBlock(IBlock block, out LogEvent logEvent)
//        {
//            logEvent = new LogEvent
//            {
//                //TODO: set address in other place
//                //Address = ContractHelpers.GetCrossChainContractAddress(block.Header.ChainId),
//                Topics =
//                {
//                    ByteString.CopyFrom(CrossChainConsts.CrossChainIndexingEventName.CalculateHash())
//                }
//            };
////            try
////            {
////                return logEvent.GetBloom().IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
////            }
////            catch (Exception)
////            {
////                return false;
////            }
//            // todo disable bloom filter and improvement needed
//            return true;
//        }
//
//        public static Hash TryGetValidateCrossChainBlockData(TransactionResult res, IBlock block,
//            LogEvent interestedLogEvent,
//            out CrossChainBlockData crossChainBlockData)
//        {
//            crossChainBlockData = null;
//
//            object[] indexingEventData = ExtractCrossChainBlockDataFromEvent(res.Logs, interestedLogEvent);
//            if (indexingEventData == null)
//                return null;
//            var senderInEvent = (Address) indexingEventData[2];
//            var recoveredRes = CryptoHelpers.RecoverPublicKey(block.Header.Sig.ToByteArray(),
//                block.GetHash().DumpByteArray(), out var producerPubKey);
//            if (!recoveredRes || !Address.FromPublicKey(producerPubKey).Equals(senderInEvent))
//                return null; // only valid transaction from current BP
//
//            crossChainBlockData = (CrossChainBlockData) indexingEventData[1];
//            return (Hash) indexingEventData[0];
//        }
//
//        private static object[] ExtractCrossChainBlockDataFromEvent(IEnumerable<LogEvent> logEvents,
//            LogEvent interestedLogEvent)
//        {
//            var targetLogEvent = logEvents.FirstOrDefault(logEvent => logEvent.HasSameTopicWith(interestedLogEvent));
//            if (targetLogEvent == null)
//                return null;
//            object[] indexingEventData = ParamsPacker.Unpack(targetLogEvent.Data.ToByteArray(),
//                new[] {typeof(Hash), typeof(CrossChainBlockData), typeof(Address)});
//            return indexingEventData;
//        }
//
//        private static bool HasSameTopicWith(this LogEvent logEvent, LogEvent logEvent2)
//        {
//            return logEvent.Address.Equals(logEvent2.Address) && logEvent.Topics.Equals(logEvent2.Topics);
//        }
    }
}