using System.IO;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using Xunit;
using Console = System.Console;

namespace AElf.CLI2.Tests
{
    public class TestJSEngine
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

        private static IJSEngine GetJSEngine()
        {
            var option = new AccountNewOption {ServerAddr = "", Password = ""};
            return IoCContainerBuilder.Build(option, new UnittestBridgeJSProvider()).Resolve<IJSEngine>();
        }

        [Fact]
        public void TestJSConsole()
        {
            var jsEngine = GetJSEngine();
            Assert.NotNull(jsEngine);
            jsEngine.RunScript(@"console.log(""hello"", ""world"");");
            jsEngine.RunScript(@"console.log(1, 1.2);");
            jsEngine.RunScript(@"ok = true");
            Assert.True(jsEngine.Get("ok").Value.ToBoolean());
        }
    }
}