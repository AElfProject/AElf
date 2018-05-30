using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Contracts.Examples
{
    public class SimpleTokenContract: CSharpSmartContract
    {
        [SmartContractFieldData("Balances", DataAccessMode.AccountSpecific)]
        public Map Balances = new Map("Balances");
        
            
        [SmartContractFieldData("TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName { get; }
        public async Task<object> InitializeAsync()
        {
            await Balances.SetValueAsync("0".CalculateHash(), ((ulong)200).ToBytes());
            await Balances.SetValueAsync("1".CalculateHash(), ((ulong)100).ToBytes());
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

        public SimpleTokenContract(string tokenContractName)
        {
            TokenContractName = tokenContractName;
        }
        
        [SmartContractFunction("Transfer(string, string, ulong)", new string[]{}, new []{"Balances.${from}"})]
        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            var fromBalBytes = await Balances.GetValue(from);
            var fromBal = fromBalBytes.ToUInt64();
            var toBalBytes = await Balances.GetValue(to);
            var toBal = toBalBytes.ToUInt64();
            var newFromBal = fromBal - qty;
            if (newFromBal > 0)
            {
                var newToBal = toBal + qty;
                await Balances.SetValueAsync(from, newFromBal.ToBytes());
                await Balances.SetValueAsync(to, newToBal.ToBytes());
                return true;
            }
            else
            {
                return false;
            }
        }

        [SmartContractFunction("GetBalance(string)", new string[]{}, new []{"Balances"})]
        public async Task<object> GetBalance(Hash account)
        {
            var balBytes = await Balances.GetValue(account.CalculateHash());
            Api.LogToResult(balBytes);
            return balBytes.ToUInt64();
        }
    }
}