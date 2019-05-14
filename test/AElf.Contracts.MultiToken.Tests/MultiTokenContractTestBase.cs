using System.Linq;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;

namespace AElf.Contracts.MultiToken
 {
     public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
     {
         public byte[] DividendContractCode => Codes.Single(kv => kv.Key.Contains("Dividend")).Value;
         public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
         protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
         protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
     }
 }