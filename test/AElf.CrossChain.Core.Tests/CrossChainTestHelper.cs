using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS7;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<int, long> _sideChainIdHeights = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();

        private GetIndexingProposalStatusOutput _pendingCrossChainIndexingProposalOutput =
            new GetIndexingProposalStatusOutput();
        
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

        internal void AddFakePendingCrossChainIndexingProposal(int chainId, PendingChainIndexingProposalStatus pendingCrossChainIndexingProposalOutput)
        {
            _pendingCrossChainIndexingProposalOutput.ChainIndexingProposalStatus[chainId] =
                pendingCrossChainIndexingProposalOutput;
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
            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetParentChainId))
            {
                var parentChainId = _parentChainIdHeight.Keys.FirstOrDefault();
                if (parentChainId != 0)
                    return new Int32Value {Value = parentChainId}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return null;
            }
            
            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetParentChainHeight))
            {
                return _parentChainIdHeight.Count == 0
                    ? null
                    : new Int64Value {Value = _parentChainIdHeight.Values.First()}.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetSideChainHeight))
            {
                int sideChainId = Int32Value.Parser.ParseFrom(transaction.Params).Value;
                var exist = _sideChainIdHeights.TryGetValue(sideChainId, out var sideChainHeight);
                if (exist)
                    return new Int64Value{Value = sideChainHeight}.ToByteArray();
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new Int64Value().ToByteArray();
            }

            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetAllChainsIdAndHeight))
            {
                var dict = new ChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                dict.IdHeightDict.Add(_parentChainIdHeight);
                return dict.ToByteArray();
            }

            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetSideChainIdAndHeight))
            {
                var dict = new ChainIdAndHeightDict();
                dict.IdHeightDict.Add(_sideChainIdHeights);
                return dict.ToByteArray();
            }
            
            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetIndexedSideChainBlockDataByHeight))
            {
                long height = Int64Value.Parser.ParseFrom(transaction.Params).Value;
                if (_indexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData))
                {
                    var indexedSideChainBlockData = new IndexedSideChainBlockData
                    {
                        SideChainBlockDataList = {crossChainBlockData.SideChainBlockDataList}
                    };
                    return indexedSideChainBlockData.ToByteArray();
                }
                
                trace.ExecutionStatus = ExecutionStatus.ContractError;
                return new IndexedSideChainBlockData().ToByteArray();
            }

            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub.GetSideChainIndexingInformationList))
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

            if (methodName == nameof(CrossChainContractImplContainer.CrossChainContractImplStub
                    .GetIndexingProposalStatus))
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