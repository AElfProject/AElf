using System;
using System.IO;
using System.Linq;
using AElf.Runtime.CSharp.Tests.TestContract;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Core
{
    public class ContractCodeLoadContextTests : CSharpRuntimeCoreTestBase
    {
        [Fact]
        public void Load_Test()
        {
            var sdkDir = Path.GetDirectoryName(typeof(SdkStreamManager).Assembly.Location);
            var sdkStreamManager = new SdkStreamManager(sdkDir);
            var loader = new ContractCodeLoadContext(sdkStreamManager);

            var code = File.ReadAllBytes(typeof(TestContract).Assembly.Location);
            using (var stream = new MemoryStream(code))
            {
                var assembly = loader.LoadFromStream(stream);
                Activator.CreateInstance(assembly.GetType("AElf.Runtime.CSharp.Tests.TestContract.TestContract"));

                assembly.FullName.ShouldBe(
                    "AElf.Runtime.CSharp.Tests.TestContract, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

                loader.Assemblies.Count().ShouldBe(2);
                loader.Assemblies.ShouldContain(a =>
                    a.FullName ==
                    "AElf.Runtime.CSharp.Tests.TestContract, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                loader.Assemblies.ShouldContain(a =>
                    a.FullName == "AElf.Sdk.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            }

            loader.Unload();
        }
    }
}