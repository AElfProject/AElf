using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Api.CSharp;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Metadata;
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
            var balBytes = await Balances.GetValue(account);
            return balBytes.ToUInt64();
        }
    }
}