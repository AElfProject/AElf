using System;
using System.IO;
using AElf.Common;
using AElf.Contracts.Token;
using AElf.Kernel;
using AElf.Kernel.ABI;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp.Core;
using AElf.Runtime.CSharp.Core.ABI;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class MethodCacheTests: CSharpRuntimeTestBase
    {
        private Module Module { get; set; }
        private MethodsCache Cache { get; set; }

        public MethodCacheTests()
        {
            var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            Module = Generator.GetABIModule(contractCode);
            var instance = new TokenContract();
            Cache = new MethodsCache(Module, instance);
        }

        [Fact]
        public void Get_Exist_MethodAbi()
        {
            var method = Cache.GetMethodAbi(nameof(TokenContract.Transfer));
            method.ShouldNotBeNull();
            method.Name.ShouldBe(nameof(TokenContract.Transfer));
            method.Params.Count.ShouldBe(2);
            method.ReturnType.ShouldBe("void");
        }

        [Fact]
        public void Get_NonExist_Method()
        {
            Should.Throw<InvalidMethodNameException>(() => Cache.GetMethodAbi("TestMethod"));
        }

        [Fact]
        public void Get_ExistHandler()
        {
            var handler = Cache.GetHandler(nameof(TokenContract.TransferFrom));
            handler.ShouldNotBe(null);
        }

        [Fact]
        public void Get_NonExist_Handler()
        {
            Should.Throw<InvalidMethodNameException>(() => Cache.GetHandler("TestMethod"));
        }
    }
}