using System;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Benchmark.TestContract
{
    public class TestTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public readonly MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");
        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public StringField TokenContractName;
        
        public bool Initialize(string tokenContractName, Hash owner)
        {
            TokenContractName.SetValue(tokenContractName);
            return true;
        }
        
        [SmartContractFunction("${this}.InitBalance", new string[]{}, new []{"${this}.Balances"})]
        public bool InitBalance(Hash addr)
        {
            //Console.WriteLine("InitBalance " + addr);
            ulong initBalance = 10000;
            Balances.SetValue(addr, initBalance);
            var fromBal = Balances.GetValue(addr);
            //Console.WriteLine("Read from db of account " + addr + " with balance " + fromBal);
            return true;
        }
        
        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances"})]
        public bool Transfer(Hash from, Hash to, ulong qty)
        {
            
            var fromBal = Balances.GetValue(from);
            //Console.WriteLine("from pass");
            var toBal = Balances.GetValue(to);
            //Console.WriteLine("to pass");
            var newFromBal = fromBal + 1;
            //Api.Assert(fromBal > qty);
            
            var newToBal = toBal + qty;
            
            Balances.SetValue(from, newFromBal);
            //Console.WriteLine("set from pass");
            Balances.SetValue(to, newToBal);
            //Console.WriteLine("set to pass");
            //Console.WriteLine($"After transfer: {from.Value.ToByteArray().ToHex()} - {newFromBal} || {to.Value.ToByteArray().ToHex()} - {newToBal}");
            return true;
        }

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public ulong GetBalance(Hash account)
        {
            return Balances.GetValue(account);
        }
    }
}