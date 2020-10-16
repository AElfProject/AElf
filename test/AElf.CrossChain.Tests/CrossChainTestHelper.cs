using System.Collections.Generic;
using AElf.Standards.ACS7;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<Hash, CrossChainTransactionInput> _fakeCrossChainBlockData =
            new Dictionary<Hash, CrossChainTransactionInput>();

        private readonly Dictionary<Hash, CrossChainExtraData> _fakeCrossChainExtraData =
            new Dictionary<Hash, CrossChainExtraData>();

        private readonly Dictionary<long, CrossChainBlockData> _fakeIndexedCrossChainBlockData =
            new Dictionary<long, CrossChainBlockData>();

        private readonly Dictionary<int, long> _chainIdHeight = new Dictionary<int, long>();

        public void AddFakeCrossChainTransactionInput(Hash previousHash,
            CrossChainTransactionInput crossChainTransactionInput)
        {
            _fakeCrossChainBlockData.Add(previousHash, crossChainTransactionInput);
        }

        public CrossChainTransactionInput GetCrossChainBlockData(Hash previousHash)
        {
            return _fakeCrossChainBlockData.TryGetValue(previousHash, out var chainTransactionInput)
                ? chainTransactionInput
                : null;
        }

        public void AddFakeExtraData(Hash previousHash, CrossChainExtraData crossChainExtraData)
        {
            _fakeCrossChainExtraData.Add(previousHash, crossChainExtraData);
        }

        public CrossChainExtraData GetCrossChainExtraData(Hash previousHash)
        {
            return _fakeCrossChainExtraData.TryGetValue(previousHash, out var crossChainExtraData)
                ? crossChainExtraData
                : null;
        }

        public void AddFakeIndexedCrossChainBlockData(long height, CrossChainBlockData crossChainBlockData)
        {
            _fakeIndexedCrossChainBlockData.Add(height, crossChainBlockData);
        }

        public CrossChainBlockData GetIndexedCrossChainExtraData(long height)
        {
            return _fakeIndexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public void AddFakeChainIdHeight(int chainId, long libHeight)
        {
            _chainIdHeight.Add(chainId, libHeight);
        }

        public ChainIdAndHeightDict GetAllIndexedCrossChainExtraData()
        {
            var sideChainIdAndHeightDict = new ChainIdAndHeightDict
            {
                IdHeightDict = {_chainIdHeight}
            };
            return sideChainIdAndHeightDict;
        }
    }
}