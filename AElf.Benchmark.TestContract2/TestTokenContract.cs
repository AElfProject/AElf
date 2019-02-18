using System;
using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Benchmark.TestContract
{
    public class TestTokenContract : CSharpSmartContract<TestTokenContractState>
    {
        public bool InitBalance(Address addr)
        {
            // Console.WriteLine("InitBalance " + addr);
            ulong initBalance = ulong.MaxValue - 1;
            State.Balances[addr] = initBalance;
            var fromBal = State.Balances[addr];
            // Console.WriteLine("Read from db of account " + addr + " with balance " + fromBal);
            return true;
        }
        
        public bool Transfer(Address from, Address to, ulong qty)
        {
            var fromBal = State.Balances[from];
            // Console.WriteLine("from pass");

            var toBal = State.Balances[to];
            // Console.WriteLine("to pass");
            var newFromBal = fromBal - qty;
            Assert(fromBal > qty, $"Insufficient balance, {qty} is required but there is only {fromBal}.");
            
            var newToBal = toBal + qty;

            State.Balances[from] = newFromBal;
            // Console.WriteLine("set from pass");

            State.Balances[to] = newToBal;
            // Console.WriteLine("set to pass");
            // Console.WriteLine($"After transfer: {from.DumpHex()} - {newFromBal} || {to.DumpHex()} - {newToBal}");
            return true;
        }

        public ulong GetBalance(Address account)
        {
            // Console.WriteLine("Getting balance.");
            return State.Balances[account];
        }
    }
}