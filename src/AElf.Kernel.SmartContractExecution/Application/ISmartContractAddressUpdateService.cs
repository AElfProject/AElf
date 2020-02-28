using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ISmartContractAddressUpdateService
    {
        Task UpdateSmartContractAddressesAsync(BlockHeader blockHeader);
    }

    public class SmartContractAddressUpdateService : ISmartContractAddressUpdateService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionExecutingService;

        private readonly IEnumerable<ISmartContractAddressNameProvider> _smartContractAddressNameProviders;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractAddressUpdateService(
            IEnumerable<ISmartContractAddressNameProvider> smartContractAddressNameProviders,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionExecutingService)
        {
            _smartContractAddressNameProviders = smartContractAddressNameProviders;
            _smartContractAddressService = smartContractAddressService;
            _transactionExecutingService = transactionExecutingService;
        }

        public async Task UpdateSmartContractAddressesAsync(BlockHeader blockHeader)
        {
            foreach (var smartContractAddressNameProvider in _smartContractAddressNameProviders)
            {
                await UpdateSmartContractAddressesAsync(blockHeader, smartContractAddressNameProvider);
            }
        }

        private async Task UpdateSmartContractAddressesAsync(BlockHeader blockHeader,
            ISmartContractAddressNameProvider smartContractAddressNameProvider)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockHeader.GetHash(),
                BlockHeight = blockHeader.Height
            };
            var returnValue = await _transactionExecutingService.ExecuteTransactionAsync(chainContext,
                _smartContractAddressService.GetZeroSmartContractAddress(),
                _smartContractAddressService.GetZeroSmartContractAddress(),
                nameof(Acs0.ACS0Container.ACS0Stub.GetContractAddressByName),
                smartContractAddressNameProvider.ContractName.ToByteString());

            var address = Address.Parser.ParseFrom(returnValue);
            if (!address.Value.IsEmpty)
                _smartContractAddressService.SetAddress(smartContractAddressNameProvider.ContractName, address);
        }
    }
}