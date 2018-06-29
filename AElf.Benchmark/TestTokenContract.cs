using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Benchmark
{
    public class TestTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public readonly MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");

        [SmartContractFieldData("${this}.TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public StringField TokenContractName;
        
        public bool Initialize(string tokenContractName, Hash owner)
        {
            Console.WriteLine("Initialize " + tokenContractName + " " + owner.Value.ToBase64());
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
            //Console.WriteLine("Transfer " + from.Value.ToBase64() + " , " + to.Value.ToBase64());
            
            var fromBal = Balances.GetValue(from);
            //Console.WriteLine("from pass");
            var toBal = Balances.GetValue(to);
            //Console.WriteLine("to pass");
            var newFromBal = fromBal - qty;
            if (fromBal >= qty)
            {
                var newToBal = toBal + qty;
                
                Balances.SetValue(from, newFromBal);
                //Console.WriteLine("set from pass");
                Balances.SetValue(to, newToBal);
                //Console.WriteLine("set to pass");

                Console.WriteLine("After transfer: from- " + from.Value.ToBase64() + " (" + newFromBal +") to- " 
                + to.Value.ToBase64() + "(" + newToBal + ")");
                return true;
            }
            else
            {
                //Console.WriteLine("Not enough balance newFromBal " + newFromBal + " < 0");
                return false;
            }
        }

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public ulong GetBalance(Hash account)
        {
            return Balances.GetValue(account);
        }
    }
}