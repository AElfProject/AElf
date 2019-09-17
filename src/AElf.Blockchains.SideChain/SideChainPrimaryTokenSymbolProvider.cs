using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.SideChain
{
    internal class SideChainPrimaryTokenSymbolProvider : IPrimaryTokenSymbolProvider
    {
        private readonly ITokenContractReaderFactory _tokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private string _primaryTokenSymbol;

        public SideChainPrimaryTokenSymbolProvider(ITokenContractReaderFactory tokenContractReaderFactory,
            IBlockchainService blockchainService)
        {
            _tokenContractReaderFactory = tokenContractReaderFactory;
            _blockchainService = blockchainService;
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
            _primaryTokenSymbol = (await _tokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            }).GetPrimaryTokenSymbol.CallAsync(new Empty())).Value;

            return _primaryTokenSymbol;
        }
    }
}