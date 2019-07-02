using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
    {
        public byte[] DividendContractCode => Codes.Single(kv => kv.Key.Contains("Dividend")).Value;
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub;
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
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
    }
}