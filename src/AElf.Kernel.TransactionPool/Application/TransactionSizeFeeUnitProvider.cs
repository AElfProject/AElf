using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionSizeFeeUnitProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;

        public ILogger<TransactionSizeFeeUnitProvider> Logger { get; set; }

        private long? _unitPrice;

        public TransactionSizeFeeUnitProvider(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;

            Logger = NullLogger<TransactionSizeFeeUnitProvider>.Instance;
        }

        public void SetUnitPrice(long unitPrice)
        {
            Logger.LogTrace($"Set tx size fee unit price: {unitPrice}");
            _unitPrice = unitPrice;
        }

        public async Task<long> GetUnitPriceAsync()
        {
            if (_unitPrice != null)
            {
//                Logger.LogTrace($"Get tx size fee unit price: {_unitPrice.Value}");
                return _unitPrice.Value;
            }

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });

            _unitPrice = (await tokenStub.GetTransactionSizeFeeUnitPrice.CallAsync(new Empty())).Value;

            Logger.LogTrace($"Get tx size fee unit price: {_unitPrice.Value}");

            return _unitPrice.Value;
        }
    }
}