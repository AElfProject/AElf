using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;

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
        protected Address User2Address => Address.FromPublicKey(User2KeyPair.PublicKey);
        protected const string DefaultSymbol = "ELF";
    }
}