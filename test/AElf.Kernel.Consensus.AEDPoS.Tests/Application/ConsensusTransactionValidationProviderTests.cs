using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class ConsensusTransactionValidationProviderTests : AEDPoSTestBase
    {
        private readonly IConstrainedTransactionValidationProvider _constrainedTransactionValidationProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;

        public ConsensusTransactionValidationProviderTests()
        {
            _constrainedTransactionValidationProvider = GetRequiredService<IConstrainedTransactionValidationProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }
        
        [Fact]
        public  void Validate_Consensus_Transaction_Test()
        {
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey),
                To = GetConsensusContractAddress(),
                MethodName = "FirstRound",
                Params = ByteString.CopyFromUtf8("test"),
            };
            var hash = Hash.FromString("test1");
            var result = _constrainedTransactionValidationProvider.ValidateTransaction(transaction, hash);
            result.ShouldBeTrue();

            transaction.MethodName = "UpdateValue";
            var result1 = _constrainedTransactionValidationProvider.ValidateTransaction(transaction, hash);
            result1.ShouldBeFalse();
            
            //remove block hash and verify again 
            _constrainedTransactionValidationProvider.ValidateTransaction(transaction, hash);
            _constrainedTransactionValidationProvider.ClearBlockHash(hash);
            var result2 = _constrainedTransactionValidationProvider.ValidateTransaction(transaction, hash);
            result2.ShouldBeTrue();
        }

        private Address GetConsensusContractAddress()
        {
            return _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
        }
    }
}