using System.IO;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using AElf.CLI2.SDK;
using AElf.CLI2.Tests.Utils;
using Autofac;
using Xunit;
using Xunit.Abstractions;

namespace AElf.CLI2.Tests
{
    public class TestSdk
    {
        private class UnittestBridgeJSProvider : IBridgeJSProvider
        {
            public Stream GetBridgeJSStream()
            {
                var location = Assembly.GetAssembly(typeof(IoCContainerBuilder)).Location;
                // NOTE: here we could inject some unittest special javascript files.
                return Assembly.LoadFrom(location).GetManifestResourceStream(BridgeJSProvider.BridgeJSResourceName);
            }
        }
        private readonly ITestOutputHelper _output;

        public TestSdk(ITestOutputHelper output)
        {
            this._output = output;
        }
        
        private IAElfSdk GetSdk()
        {
            var option = new AccountOption
            {
                ServerAddr = "http://localhost:5000/aelf/api",
                Password = "",
                Action = AccountAction.create,
                AccountFileName = "a.account"
            };
            return IoCContainerBuilder.Build(option, new UnittestBridgeJSProvider(),
                new UTLogModule(_output)).Resolve<IAElfSdk>();
        }
        
        [Fact]
        public void TestConnectChain()
        {
            var sdk = GetSdk();
            sdk.Chain().ConnectChain();
        }
    }
}