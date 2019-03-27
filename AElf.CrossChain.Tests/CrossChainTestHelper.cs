using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<int, long> _sideChainIdHeights = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();
        public long FakeLibHeight { get; private set;}
        private readonly Dictionary<long, CrossChainBlockData> _indexedCrossChainBlockData = new Dictionary<long, CrossChainBlockData>();
        public void AddFakeSideChainIdHeight(int sideChainId, long height)
        {
            _sideChainIdHeights.Add(sideChainId, height);
        }

        public void AddFakeParentChainIdHeight(int parentChainId, long height)
        {
            _parentChainIdHeight.Add(parentChainId, height);
        }

        public void AddFakeIndexedCrossChainBlockData(long height, CrossChainBlockData crossChainBlockData)
        {
            _indexedCrossChainBlockData.Add(height, crossChainBlockData);
        }

        public TransactionTrace CreateFakeTransactionTrace(Transaction transaction)
        {
            string methodName = transaction.MethodName;

            var trace = new TransactionTrace
            {
                TransactionId = transaction.GetHash(),
                ExecutionStatus = ExecutionStatus.Executed,
            };
            var returnValue = CreateFakeReturnValue(trace, transaction, methodName);
            trace.ReturnValue = returnValue == null ? ByteString.Empty : ByteString.CopyFrom(returnValue);
            
            return trace;
        }

        private byte[] CreateFakeReturnValue(TransactionTrace trace, Transaction transaction, string methodName)
        {
            if (methodName == nameof(CrossChainContractMethodNames.GetParentChainId))
            {
                var parentChainId = _parentChainIdHeight.Keys.FirstOrDefault();
                if (parentChainId != 0)
                    return new SInt32Value {Value = parentChainId}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }
            
            if (methodName == nameof(CrossChainContractMethodNames.GetParentChainHeight))
            {
                return _parentChainIdHeight.Count == 0
                    ? null
                    : new SInt64Value {Value = _parentChainIdHeight.Values.First()}.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractMethodNames.GetSideChainHeight))
            {
                int sideChainId = SInt32Value.Parser.ParseFrom(transaction.Params).Value;
                var exist = _sideChainIdHeights.TryGetValue(sideChainId, out var sideChainHeight);
                if (exist)
                    return new SInt64Value{Value = sideChainHeight}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new SInt64Value().ToByteArray();
            }

            if (methodName == nameof(CrossChainContractMethodNames.GetAllChainsIdAndHeight))
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                dict.IdHeightDict.Add(_parentChainIdHeight);
                return dict.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractMethodNames.GetSideChainIdAndHeight))
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                return dict.ToByteArray();
            }
            
            if (methodName == nameof(CrossChainContractMethodNames.GetIndexedCrossChainBlockDataByHeight))
            {
                long height = SInt64Value.Parser.ParseFrom(transaction.Params).Value;
                if (_indexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData))
                    return crossChainBlockData.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new CrossChainBlockData().ToByteArray();
            }
            return new byte[0];
        }
        public void SetFakeLibHeight(long height)
        {
            FakeLibHeight = height;
        }
    }
}