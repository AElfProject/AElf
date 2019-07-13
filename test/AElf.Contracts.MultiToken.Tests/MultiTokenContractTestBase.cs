using System;
using System.Linq;
using Acs2;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
    {
        protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
        protected long BobCoinTotalAmout => 1_000_000_000_0000L;
        protected byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub;
        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultAddress => Address.FromPublicKey(DefaultKeyPair.PublicKey);
        protected ECKeyPair User1KeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address User1Address => Address.FromPublicKey(User1KeyPair.PublicKey);
        protected ECKeyPair User2KeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[12];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected Address User2Address => Address.FromPublicKey(User2KeyPair.PublicKey);
        protected const string DefaultSymbol = "ELF";
        public byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
        protected Address TreasuryContractAddress { get; set; }

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;
        public byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        protected Address ProfitContractAddress { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub;
        public byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
        protected Address TokenConverterContractAddress { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub;

        internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;
        
        protected Address BasicFunctionContractAddress { get; set; }
        
        protected Address OtherBasicFunctionContractAddress { get; set; }
        
        internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }
        
        internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        protected byte[] BasicFunctionContractCode => Codes.Single(kv => kv.Key.Contains("BasicFunction")).Value;
        protected Hash BasicFunctionContractName => Hash.FromString("AElf.TestContractNames.BasicFunction");
        protected Hash OtherBasicFunctionContractName => Hash.FromString("AElf.TestContractNames.OtherBasicFunction");
        
        protected readonly Address _address = Address.Generate();
        
        protected const string SymbolForTest = "ELFTEST";
        
        protected const long Amount = 100;
        protected void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
    }
}