using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionSizeFeeUnitProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;

        private long? _unitPrice;

        public TransactionSizeFeeUnitProvider(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
        }

        public void SetUnitPrice(long unitPrice)
        {
            _unitPrice = unitPrice;
        }

        public async Task<long> GetUnitPriceAsync()
        {
            if (_unitPrice != null) return _unitPrice.Value;

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });

            return (await tokenStub.GetTransactionSizeFeeUnitPrice.CallAsync(new Empty())).Value;
        }
    }
}