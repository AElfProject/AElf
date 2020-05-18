using System.Collections.Generic;
using System.Linq;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<int, long> _sideChainIdHeights = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();
        private GetPendingCrossChainIndexingProposalOutput _pendingCrossChainIndexingProposalOutput;
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

        internal void AddFakeIndexedCrossChainBlockData(long height, CrossChainBlockData crossChainBlockData)
        {
            _indexedCrossChainBlockData.Add(height, crossChainBlockData);
        }

        internal void AddFakePendingCrossChainIndexingProposal(GetPendingCrossChainIndexingProposalOutput pendingCrossChainIndexingProposalOutput)
        {
            _pendingCrossChainIndexingProposalOutput = pendingCrossChainIndexingProposalOutput;
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
            if (returnValue == null)
                trace.ExecutionStatus = ExecutionStatus.ContractError;
            else 
                trace.ReturnValue = ByteString.CopyFrom(returnValue);
            
            return trace;
        }

        private byte[] CreateFakeReturnValue(TransactionTrace trace, Transaction transaction, string methodName)
        {
            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetParentChainId))
            {
                var parentChainId = _parentChainIdHeight.Keys.FirstOrDefault();
                if (parentChainId != 0)
                    return new Int32Value {Value = parentChainId}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }
            
            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetParentChainHeight))
            {
                return _parentChainIdHeight.Count == 0
                    ? null
                    : new Int64Value {Value = _parentChainIdHeight.Values.First()}.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetSideChainHeight))
            {
                int sideChainId = Int32Value.Parser.ParseFrom(transaction.Params).Value;
                var exist = _sideChainIdHeights.TryGetValue(sideChainId, out var sideChainHeight);
                if (exist)
                    return new Int64Value{Value = sideChainHeight}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new Int64Value().ToByteArray();
            }

            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetAllChainsIdAndHeight))
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                dict.IdHeightDict.Add(_parentChainIdHeight);
                return dict.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetSideChainIdAndHeight))
            {
                var dict = new SideChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                return dict.ToByteArray();
            }
            
            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight))
            {
                long height = Int64Value.Parser.ParseFrom(transaction.Params).Value;
                if (_indexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData))
                    return crossChainBlockData.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new CrossChainBlockData().ToByteArray();
            }

            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub.GetSideChainIndexingInformationList))
            {
                var sideChainIndexingInformationList = new SideChainIndexingInformationList();
                foreach (var kv in _sideChainIdHeights)
                {
                    sideChainIndexingInformationList.IndexingInformationList.Add(new SideChainIndexingInformation
                    {
                        ChainId = kv.Key,
                        IndexedHeight = kv.Value
                    });
                }
                
                return sideChainIndexingInformationList.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractContainer.CrossChainContractStub
                    .GetPendingCrossChainIndexingProposal))
            {
                return _pendingCrossChainIndexingProposalOutput?.ToByteArray();
            }
            
            return new byte[0];
        }
        
        public void SetFakeLibHeight(long height)
        {
            FakeLibHeight = height;
        }
    }
}