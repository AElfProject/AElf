using System.IO;
using AElf.Kernel.ABI;
using AElf.Runtime.CSharp.Core;
using AElf.Runtime.CSharp.Core.ABI;
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
            var contractCode = File.ReadAllBytes(typeof(TestContract.TestContract).Assembly.Location);
            Module = Generator.GetABIModule(contractCode);
            var instance = new TestContract.TestContract();
            Cache = new MethodsCache(Module, instance);
        }

        [Fact]
        public void Get_Exist_MethodAbi()
        {
            var method = Cache.GetMethodAbi(nameof(TestContract.TestContract.TestBoolState));
            method.ShouldNotBeNull();
            method.Name.ShouldBe(nameof(TestContract.TestContract.TestBoolState));
            method.Params.Count.ShouldBe(1);
            method.Params[0].Type.ShouldBe("AElf.Runtime.CSharp.Tests.TestContract.BoolInput");
            method.ReturnType.ShouldBe("AElf.Runtime.CSharp.Tests.TestContract.BoolOutput");
        }

        [Fact]
        public void Get_NonExist_Method()
        {
            Should.Throw<InvalidMethodNameException>(() => Cache.GetMethodAbi("TestMethod"));
        }

        [Fact]
        public void Get_ExistHandler()
        {
            var handler = Cache.GetHandler(nameof(TestContract.TestContract.TestStringState));
            handler.ShouldNotBe(null);
        }

        [Fact]
        public void Get_NonExist_Handler()
        {
            Should.Throw<InvalidMethodNameException>(() => Cache.GetHandler("TestMethod"));
        }
    }
}