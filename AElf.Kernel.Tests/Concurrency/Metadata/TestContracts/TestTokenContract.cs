using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    public class TestTokenContract: CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");


        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName;
        public async Task<object> InitializeAsync()
        {
            await Balances.SetValueAsync("0".CalculateHash(), 200);
            await Balances.SetValueAsync("1".CalculateHash(), 100);
            return null;
        }
        
        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();
            
            // invoke
            await (Task<object>) member.Invoke(this, parameters);
        }

        public TestTokenContract(string tokenContractName)
        {
            TokenContractName = tokenContractName;
        }
        
        [SmartContractFunction("${this}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)", new string[]{}, new []{"${this}.Balances"})]
        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            var fromBal = await Balances.GetValueAsync(from);
            var toBal = await Balances.GetValueAsync(to);
            var newFromBal = fromBal - qty;
            if (newFromBal > 0)
            {
                var newToBal = toBal + qty;
                await Balances.SetValueAsync(from, newFromBal);
                await Balances.SetValueAsync(to, newToBal);
                return true;
            }
            else
            {
                return false;
            }
        }

        [SmartContractFunction("${this}.GetBalance(AElf.Kernel.Hash)", new string[]{}, new []{"${this}.Balances"})]
        public async Task<object> GetBalance(Hash account)
        {
            var bal= await Balances.GetValueAsync(account.CalculateHash());
            Api.Return(new UInt64Value() { Value = bal});
            return bal;
        }
    }
}