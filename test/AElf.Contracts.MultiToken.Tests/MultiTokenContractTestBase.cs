using System.Linq;
using AElf.Contracts.TestBase;
 
 namespace AElf.Contracts.MultiToken
 {
     public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
     {
         public byte[] DividendContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
         public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
     }
 }