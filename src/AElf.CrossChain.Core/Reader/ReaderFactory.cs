// using AElf.Contracts.CrossChain;
// using AElf.Kernel;
// using AElf.Kernel.SmartContract.Application;
// using AElf.Types;
// using Google.Protobuf.WellKnownTypes;
//
// namespace AElf.CrossChain
// {
//     internal interface IReaderFactory
//     {
//         CrossChainContractContainer.CrossChainContractStub Create(Hash blockHash, long blockHeight, Timestamp timestamp = null);
//     }
//
//     internal class ReaderFactory : IReaderFactory
//     {    
//         private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
//         private readonly ISmartContractAddressService _smartContractAddressService;
//
//         public ReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
//             ISmartContractAddressService smartContractAddressService)
//         {
//             _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
//             _smartContractAddressService = smartContractAddressService;
//         }
//         
//         public CrossChainContractContainer.CrossChainContractStub Create(Hash blockHash, long blockHeight, Timestamp timestamp = null)
//         {
//             return new CrossChainContractContainer.CrossChainContractStub()
//             {
//                 __factory = new MethodStubFactory(_transactionReadOnlyExecutionService, _smartContractAddressService,
//                     new ChainContext()
//                     {
//                         BlockHash = blockHash,
//                         BlockHeight = blockHeight
//                     }, timestamp ?? TimestampHelper.GetUtcNow())
//             };
//         }
//     }
// }