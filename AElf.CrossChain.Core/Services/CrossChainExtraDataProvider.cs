using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public class CrossChainExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IEnumerable<IBlockExtraDataProvider> _blockExtraDataProviders;
        private readonly ITransactionResultQueryService _transactionResultQueryService;

        public CrossChainExtraDataProvider(ITransactionResultQueryService transactionResultQueryService, 
            ICrossChainService crossChainService, IEnumerable<IBlockExtraDataProvider> blockExtraDataProviders)
        {
            _transactionResultQueryService = transactionResultQueryService;
            _crossChainService = crossChainService;
            _blockExtraDataProviders = blockExtraDataProviders;
        }

        public Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            throw new NotImplementedException();
        }
    }
}