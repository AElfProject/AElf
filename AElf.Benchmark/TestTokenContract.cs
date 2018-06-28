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
        
        public async Task<bool> InitializeAsync(string tokenContractName, Hash owner)
        {
            Console.WriteLine("InitializeAsync " + tokenContractName + " " + owner.Value.ToBase64());
            await TokenContractName.SetAsync(tokenContractName);
            return true;
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
            //Console.WriteLine("Transfer " + from.Value.ToBase64() + " , " + to.Value.ToBase64());
            
            var fromBal = await Balances.GetValueAsync(from);
            //Console.WriteLine("from pass");
            var toBal = await Balances.GetValueAsync(to);
            //Console.WriteLine("to pass");
            var newFromBal = fromBal - qty;
            if (newFromBal >= 0)
            {
                var newToBal = toBal + qty;
                
                await Balances.SetValueAsync(from, newFromBal);
                //Console.WriteLine("set from pass");
                await Balances.SetValueAsync(to, newToBal);
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
        public async Task<ulong> GetBalance(Hash account)
        {
            return await Balances.GetValueAsync(account);
        }
    }
}