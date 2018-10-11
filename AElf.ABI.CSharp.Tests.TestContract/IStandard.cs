using System;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.ABI.CSharp;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using AElf.Common;

namespace AElf.ABI.CSharp.Tests
{
    public class Transfered : AElf.Sdk.CSharp.Event
    {
        public Address From { get; set; }
        public Address To { get; set; }
        public uint Value { get; set; }
    }

    public interface IStandard
    {
        uint GetTotalSupply();
        uint GetBalanceOf(Address account);
        bool Transfer(Address to, uint value);
    }
}
