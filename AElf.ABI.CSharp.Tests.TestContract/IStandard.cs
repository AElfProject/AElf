using System;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.ABI.CSharp;
using AElf.Kernel.Types;
using AElf.Types.CSharp;

namespace AElf.ABI.CSharp.Tests
{
    public class Transfered : AElf.Sdk.CSharp.Event
    {
        public Hash From { get; set; }
        public Hash To { get; set; }
        public uint Value { get; set; }
    }

    public interface IStandard
    {
        uint GetTotalSupply();
        uint GetBalanceOf(Hash account);
        bool Transfer(Hash to, uint value);
    }
}
