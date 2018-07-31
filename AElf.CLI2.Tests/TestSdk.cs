using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.SDK;
using AElf.CLI2.Tests.Utils;
using Autofac;
using Xunit;
using Xunit.Abstractions;

namespace AElf.CLI2.Tests
{
    public class TestSdk
    {
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
            return IoCContainerBuilder.Build(option, 
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