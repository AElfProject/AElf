//using AElf.Sdk.CSharp.Types;
//using AElf.Types.CSharp.MetadataAttribute;
//using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
//using AElf.Common;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    /*
    public class TestTokenContract: CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public MapToUInt64<Address> Balances = new MapToUInt64<Address>("Balances");


        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName;
        public void Initialize()
        {
            Balances.SetValue(Address.FromString("0"), 200);
            Balances.SetValue(Address.FromString("1"), 100);
        }

        public TestTokenContract(string tokenContractName)
        {
            TokenContractName = tokenContractName;
        }
        
        [SmartContractFunction("${this}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)", new string[]{}, new []{"${this}.Balances"})]
        public bool Transfer(Address from, Address to, ulong qty)
        {
            var fromBal = Balances.GetValue(from);
            var toBal = Balances.GetValue(to);
            var newFromBal = fromBal - qty;
            if (newFromBal > 0)
            {
                var newToBal = toBal + qty;
                Balances.SetValue(from, newFromBal);
                Balances.SetValue(to, newToBal);
                return true;
            }
            else
            {
                return false;
            }
        }

        [SmartContractFunction("${this}.GetBalance(AElf.Kernel.Hash)", new string[]{}, new []{"${this}.Balances"})]
        public ulong GetBalance(Address account)
        {
            var bal= Balances.GetValue(account);
            return bal;
        }
    }
    */
}