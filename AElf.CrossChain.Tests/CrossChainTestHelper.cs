using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<int, long> _sideChainIdHeights = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();
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
                ExecutionStatus = ExecutionStatus.ExecutedButNotCommitted,
            };
            var returnValue = CreateFakeReturnValue(trace, transaction, methodName);
            trace.RetVal = new RetVal
            {
                Data = ByteString.CopyFrom(returnValue)
            };
            
            return trace;
        }

        private byte[] CreateFakeReturnValue(TransactionTrace trace, Transaction transaction, string methodName)
        {
            if (methodName == CrossChainConsts.GetParentChainIdMethodName)
            {
                var parentChainId = _parentChainIdHeight.Keys.FirstOrDefault();
                if (parentChainId != 0) 
                    return ReturnTypeHelper.GetEncoder<int>()(parentChainId);
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }
            
            if (methodName == CrossChainConsts.GetParentChainHeightMethodName)
            {
                return _parentChainIdHeight.Count == 0 ? null : ReturnTypeHelper.GetEncoder<long>()(_parentChainIdHeight.Values.First());
            }

            if (methodName == CrossChainConsts.GetSideChainHeightMethodName)
            {
                int sideChainId =
                    (int) ParamsPacker.Unpack(transaction.Params.ToByteArray(), new[] {typeof(int)})[0];
                var exist = _sideChainIdHeights.TryGetValue(sideChainId, out var sideChainHeight);
                if (exist)
                    return ReturnTypeHelper.GetEncoder<long>()(sideChainHeight);
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }

            if (methodName == CrossChainConsts.GetAllChainsIdAndHeightMethodName)
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeighDict.Add(_sideChainIdHeights);
                dict.IdHeighDict.Add(_parentChainIdHeight);
                return ReturnTypeHelper.GetEncoder<SideChainIdAndHeightDict>()(dict);
            }

            if (methodName == CrossChainConsts.GetSideChainIdAndHeightMethodName)
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeighDict.Add(_sideChainIdHeights);
                return ReturnTypeHelper.GetEncoder<SideChainIdAndHeightDict>()(dict);
            }
            
            if (methodName == CrossChainConsts.GetIndexedCrossChainBlockDataByHeight)
            {
                long height =
                    (long) ParamsPacker.Unpack(transaction.Params.ToByteArray(), new[] {typeof(long)})[0];
                if (_indexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData))
                    return ReturnTypeHelper.GetEncoder<CrossChainBlockData>()(crossChainBlockData);
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }

            return null;
        }
    }
}