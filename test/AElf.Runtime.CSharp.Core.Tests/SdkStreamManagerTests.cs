using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Core
{
    public class SdkStreamManagerTests : CSharpRuntimeCoreTestBase
    {
        
        [Fact]
        public void GetStream_SdkPathExist_Test()
        {
            var sdkDir = Path.GetDirectoryName(typeof(SdkStreamManager).Assembly.Location);
            var sdkStreamManager = new SdkStreamManager(sdkDir);
            var assemblyName = new AssemblyName("AElf.Runtime.CSharp.Core");
            
            using (var stream = sdkStreamManager.GetStream(assemblyName))
            {
                CheckGetStreamResult(stream);
            }

            // Get stream from cache
            using (var stream = sdkStreamManager.GetStream(assemblyName))
            {
                CheckGetStreamResult(stream);
            }
        }
        
        [Fact]
        public void GetStream_SdkPathNotExist_Test()
        {
            var sdkStreamManager = new SdkStreamManager("/NotExist/");
            var assemblyName = new AssemblyName("AElf.Runtime.CSharp.Core");
            
            using (var stream = sdkStreamManager.GetStream(assemblyName))
            {
                CheckGetStreamResult(stream);
            }

            // Get stream from cache
            using (var stream = sdkStreamManager.GetStream(assemblyName))
            {
                CheckGetStreamResult(stream);
            }
        }

        [Fact]
        public void ExceptionTest()
        {
            var message = "message";
            Should.Throw<InvalidMethodNameException>(() => throw new InvalidMethodNameException());
            Should.Throw<InvalidMethodNameException>(() => throw new InvalidMethodNameException(message));
            Should.Throw<RuntimeException>(() => throw new RuntimeException());
            Should.Throw<RuntimeException>(() => throw new RuntimeException(message));
            
        }

        private void CheckGetStreamResult(Stream stream)
        {
            var loader = new AssemblyLoadContext(null);
            var assembly = loader.LoadFromStream(stream);
            assembly.FullName.ShouldBe("AElf.Runtime.CSharp.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        }
    }
}