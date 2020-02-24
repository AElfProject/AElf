using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class InlineTransferFromValidationProviderTest : SmartContractRunnerTestBase
    {
        private readonly IInlineTransactionValidationProvider _inlineTransactionValidationProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        public InlineTransferFromValidationProviderTest()
        {
            _inlineTransactionValidationProvider = GetRequiredService<IInlineTransactionValidationProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }

        [Fact]
        public void Validate_Transaction_Test()
        {
            //verify non "TransferFrom" method
            var tokenAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            var tx = new Transaction
            {
                To = tokenAddress,
                MethodName = "Transfer"
            };
           var result = _inlineTransactionValidationProvider.Validate(tx);
           result.ShouldBeTrue();
           
           //verify non token contract
           tx = new Transaction
           {
               To = SampleAddress.AddressList[3],
               MethodName = "TransferFrom"
           };
           result = _inlineTransactionValidationProvider.Validate(tx);
           result.ShouldBeTrue();
           
           //verify system contract call TransferFrom
           var consensusContract =
               _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
           tx = new Transaction
           {
               From = consensusContract,
               To = tokenAddress,
               MethodName = "TransferFrom"
           };
           result = _inlineTransactionValidationProvider.Validate(tx);
           result.ShouldBeTrue();
           
           //non system contract call TransferFrom
           tx.From = SampleAddress.AddressList[3];
           result = _inlineTransactionValidationProvider.Validate(tx);
           result.ShouldBeFalse();
        }
    }
}