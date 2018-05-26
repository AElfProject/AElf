using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Api.CSharp;
using AElf.Kernel.Concurrency;
using Akka.Configuration.Hocon;
using CSharpSmartContract = AElf.Api.CSharp.CSharpSmartContract;

namespace AElf.Contracts.Examples
{
    public class SimpleTokenContract: CSharpSmartContract
    {
        [SmartContractFieldData("Balances", DataAccessMode.AccountSpecific)]
        public Map Balances = new Map("Balances");
        
        [SmartContractFieldData("TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName { get; }
        
        public override async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            Balances.SetValueAsync("0".CalculateHash(), ((ulong)200).ToBytes());
            Balances.SetValueAsync("1".CalculateHash(), ((ulong)100).ToBytes());

            await Task.CompletedTask;
        }
        
        public override async Task<object> InvokeAsync(SmartContractInvokeContext context)
        {
            var methodname = context.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(context.Params).Params.Select(p => p.Value()).ToArray();
            
            // invoke
            return await (Task<object>) member.Invoke(this, parameters);
        }

        public SimpleTokenContract(string tokenContractName)
        {
            TokenContractName = tokenContractName;
        }
        
        [SmartContractFunction("Transfer(string, string, ulong)", new []{"Balances"})]
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

        [SmartContractFunction("GetBalance(string)", new []{"Balances"})]
        public async Task<object> GetBalance(string account)
        {
            var balBytes = await Balances.GetValue(account.CalculateHash());
            return balBytes.ToUInt64();
        }
    }
}