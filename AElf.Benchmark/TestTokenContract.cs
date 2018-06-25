using System;
using System.Collections.Generic;
using System.IO;
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
        public readonly MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");

        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public StringField TokenContractName;
        
        public async Task<bool> InitializeAsync(string tokenContractName, Hash owner)
        {
            Console.WriteLine("InitializeAsync " + tokenContractName + " " + owner.Value.ToBase64());
            await TokenContractName.SetAsync(tokenContractName);
            return true;
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
        
        public async Task<bool> InitBalance(Hash addr)
        {
            //Console.WriteLine("InitBalance " + addr);
            ulong initBalance = 1000;
            await Balances.SetValueAsync(addr, initBalance);
            var fromBal = await Balances.GetValueAsync(addr);
            //Console.WriteLine("Read from db of account " + addr + " with balance " + fromBal);
            return true;
        }
        
        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances"})]
        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            return true;
        }

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public async Task<ulong> GetBalance(Hash account)
        {
            return await Balances.GetValueAsync(account);
        }
    }
}