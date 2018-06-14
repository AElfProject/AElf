using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Benchmark
{
    public class TestTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public Map Balances = new Map("Balances");

        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName;

        [SmartContractFieldData("${this}.Owner", DataAccessMode.ReadOnlyAccountSharing)]
        public Hash Owner;
        
        public async Task<object> InitializeAsync(string tokenContractName, Hash owner)
        {
            TokenContractName = tokenContractName;
            Owner = owner;
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
        
        public async Task<bool> InitBalance(Hash addr, Hash owner)
        {
            if (owner == Owner)
            {
                var balance = new UInt64Value();
                balance.Value = 10000;
                await Balances.SetValueAsync(addr, balance.ToByteArray());

                return true;
            }

            return false;
        }
        
        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances"})]
        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            var fromBalBytes = await Balances.GetValue(from);
            var toBalBytes = await Balances.GetValue(to);
            
            var fromBal = fromBalBytes.ToUInt64();
            var toBal = toBalBytes.ToUInt64();
            var newFromBal = fromBal - qty;
            if (newFromBal > 0)
            {
                var newToBal = toBal + qty;
                await Balances.SetValueAsync(from, new UInt64Value
                {
                    Value = newFromBal
                }.ToByteArray());
                await Balances.SetValueAsync(to, new UInt64Value
                {
                    Value = newToBal
                }.ToByteArray());
                return true;
            }
            else
            {
                return false;
            }
        }

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public async Task<object> GetBalance(Hash account)
        {
            var balBytes = await Balances.GetValue(account.CalculateHash());
            Api.Return(new UInt64Value() { Value = balBytes.ToUInt64() });
            return balBytes.ToUInt64();
        }
    }
}