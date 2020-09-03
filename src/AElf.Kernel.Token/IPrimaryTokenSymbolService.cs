// using System.Threading.Tasks;
// using AElf.Contracts.MultiToken;
// using AElf.Kernel.Blockchain.Application;
// using AElf.Kernel.SmartContract.Application;
// using AElf.Kernel.Token.Infrastructure;
// using Google.Protobuf.WellKnownTypes;
//
// namespace AElf.Kernel.Token
// {
//     public interface IPrimaryTokenSymbolService
//     {
//         Task<string> GetPrimaryTokenSymbol();
//     }
//
//     internal class PrimaryTokenSymbolService : IPrimaryTokenSymbolService
//     {
//         private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
//         private readonly IBlockchainService _blockchainService;
//         private readonly ISmartContractAddressService _smartContractAddressService;
//         private readonly IContractReaderFactory<TokenContractContainer.TokenContractStub> _contractReaderFactory;
//
//         public PrimaryTokenSymbolService(IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
//             IBlockchainService blockchainService, ISmartContractAddressService smartContractAddressService,
//             IContractReaderFactory<TokenContractContainer.TokenContractStub> contractReaderFactory)
//         {
//             _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
//             _blockchainService = blockchainService;
//             _smartContractAddressService = smartContractAddressService;
//             _contractReaderFactory = contractReaderFactory;
//         }
//
//         public async Task<string> GetPrimaryTokenSymbol()
//         {
//             var tokenSymbol = _primaryTokenSymbolProvider.GetPrimaryTokenSymbol();
//             if (tokenSymbol != null)
//             {
//                 return tokenSymbol;
//             }
//
//             var chain = await _blockchainService.GetChainAsync();
//             var tokenContractAddress =
//                 await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
//                 {
//                     BlockHash = chain.BestChainHash,
//                     BlockHeight = chain.BestChainHeight
//                 }, TokenSmartContractAddressNameProvider.StringName);
//             tokenSymbol = (await _contractReaderFactory.Create(new ContractReaderContext
//             {
//                 BlockHash = chain.BestChainHash,
//                 BlockHeight = chain.BestChainHeight,
//                 ContractAddress = tokenContractAddress
//             }).GetPrimaryTokenSymbol.CallAsync(new Empty())).Value;
//             _primaryTokenSymbolProvider.SetPrimaryTokenSymbol(tokenSymbol);
//
//             return tokenSymbol;
//         }
//     }
// }