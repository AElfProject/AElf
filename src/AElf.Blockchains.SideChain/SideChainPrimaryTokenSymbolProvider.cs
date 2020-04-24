using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.SideChain
{
    internal class SideChainPrimaryTokenSymbolProvider : IPrimaryTokenSymbolProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IContractReaderFactory<TokenContractContainer.TokenContractStub>
            _contractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private string _primaryTokenSymbol;

        public SideChainPrimaryTokenSymbolProvider(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<TokenContractContainer.TokenContractStub> contractReaderFactory)
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;
        }

        public void SetPrimaryTokenSymbol(string symbol)
        {
            if (_primaryTokenSymbol == null)
            {
                _primaryTokenSymbol = symbol;
            }
        }

        public async Task<string> GetPrimaryTokenSymbol()
        {
            if (_primaryTokenSymbol != null)
            {
                return _primaryTokenSymbol;
            }

            var chain = await _blockchainService.GetChainAsync();
            var tokenContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, TokenSmartContractAddressNameProvider.StringName);
            _primaryTokenSymbol = (await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight,
                ContractAddress = tokenContractAddress
            }).GetPrimaryTokenSymbol.CallAsync(new Empty())).Value;

            return _primaryTokenSymbol;
        }
    }
}