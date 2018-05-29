using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Contracts.Examples
{
    public class SimpleTokenContract: CSharpSmartContract
    {
        public Map Balances = new Map("Balances");
        
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
        
        public async Task<object> Transfer(string from, string to, ulong qty)
        {
            var fromBalBytes = await Balances.GetValue(from.CalculateHash());
            var fromBal = fromBalBytes.ToUInt64();
            var toBalBytes = await Balances.GetValue(to.CalculateHash());
            var toBal = toBalBytes.ToUInt64();
            var newFromBal = fromBal - qty;
            var newToBal = toBal + qty;
            await Balances.SetValueAsync(from.CalculateHash(), newFromBal.ToBytes());
            await Balances.SetValueAsync(to.CalculateHash(), newToBal.ToBytes());
            return null;
        }

        public async Task<object> GetBalance(string account)
        {
            var balBytes = await Balances.GetValue(account.CalculateHash());
            Api.LogToResult(balBytes);
            return balBytes.ToUInt64();
        }
    }
}