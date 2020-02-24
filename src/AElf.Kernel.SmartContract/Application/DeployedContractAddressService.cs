using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IDeployedContractAddressService
    {
        Task InitAsync();
    }

    public class DeployedContractAddressService : IDeployedContractAddressService
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());
        
        public DeployedContractAddressService(ISmartContractAddressService smartContractAddressService, 
            IBlockchainService blockchainService, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, 
            IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _deployedContractAddressProvider = deployedContractAddressProvider;
        }

        public async Task InitAsync()
        {
            var byteString = await CallContractMethodAsync(_smartContractAddressService.GetZeroSmartContractAddress(),
                "GetDeployedContractAddressList", new Empty());
            var addressList = AddressList.Parser.ParseFrom(byteString).Value.ToList();
            _deployedContractAddressProvider.Init(addressList);
        }
        
        private async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = input.ToByteString(),
                //TODO: what's SignaturePlaceholder? consider make as a constant
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var chain = await _blockchainService.GetChainAsync();
            if(chain == null) return ByteString.Empty;
            var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            }, tx, TimestampHelper.GetUtcNow());

            return transactionTrace.ReturnValue;
        }
    }
}