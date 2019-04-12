using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types.CSharp;
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
            var t = new Transaction()
            {
                From = _smartContractAddressService.GetZeroSmartContractAddress(),
                To = _smartContractAddressService.GetZeroSmartContractAddress(),
                MethodName = nameof(ISmartContractZero.GetContractAddressByName),
                Params = smartContractAddressNameProvider.ContractName.ToByteString()
            };

            var transactionResult =
                (await _transactionExecutingService.ExecuteAsync(
                    new ChainContext() {BlockHash = blockHeader.GetHash(), BlockHeight = blockHeader.Height}, t,
                    DateTime.UtcNow));

            if (!transactionResult.IsSuccessful())
                throw new InvalidOperationException();

            var address = Address.Parser.ParseFrom(transactionResult.ReturnValue);

            if (!address.Value.IsEmpty)
                _smartContractAddressService.SetAddress(smartContractAddressNameProvider.ContractName, address);
        }
    }
}