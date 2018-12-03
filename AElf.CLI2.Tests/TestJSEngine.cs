using System;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.Tests.Utils;
using Alba.CsConsoleFormat.Fluent;
using Autofac;
using Xunit;
using Xunit.Abstractions;

namespace AElf.CLI2.Tests
{
    public class TestJSEngine
    {
        private IJSEngine GetJSEngine(string serverAddress = "http://127.0.0.1:1234")
        {
            var option = new AccountOption
            {
                Endpoint = serverAddress,
                Password = "",
                Action = AccountAction.create,
                AccountFileName = "a.account"
            };
            return IoCContainerBuilder.Build(option).Resolve<IJSEngine>();
        }


        private readonly ITestOutputHelper _output;

        public TestJSEngine(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public void TestConsole()
        {
            var jsEngine = GetJSEngine();
            Assert.NotNull(jsEngine);
            jsEngine.RunScript(@"console.log(""hello"", ""world"");");
            jsEngine.RunScript(@"console.log(1, 1.2);");
            jsEngine.RunScript(@"ok = true");
            Assert.True(jsEngine.GlobalObject.ReadProperty<bool>("ok"));
        }

        [Fact]
        public void TestCrypto()
        {
            var jsEngine = GetJSEngine();
            jsEngine.RunScript(@"
                var i8 = new Uint8Array(3);
                crypto.getRandomValues(i8);
                var i8_0 = i8[0];
                var i8_1 = i8[1];
                var i8_2 = i8[2];
            ");
            // TODO: Inject a mock random generator.
            for (var i = 0; i < 3; ++i)
            {
                Assert.True(jsEngine.GlobalObject.ReadProperty<int>($"i8_{i}") < 256);
            }
        }

        [Fact]
        public void TestAelf()
        {
            var jsEngine = GetJSEngine();

            #region Execute

            // This region is to check the PrettyPrint, see Console output in Debug

            jsEngine.Execute(@"5");
            jsEngine.Execute(@"'abc'");
            jsEngine.Execute(@"var a = null; a");
            jsEngine.Execute(@"function f(v){return 5;} f;");
            jsEngine.Execute(@"var o = new Object(); o.love = '1'; o.xyz = function(x){return 1;}; o");
            jsEngine.Execute(@"var arr = [a, f, o]; arr");
            jsEngine.Execute(@"var arr = []; arr");
            jsEngine.Execute(@"var tf = false; tf");
            jsEngine.Execute(@"Aelf = require('aelf');");
            jsEngine.Execute(@"var aelf = new Aelf(_requestor);");
            jsEngine.Execute("Aelf.wallet.createNewWallet()");

            // This requires a node to be set up
            jsEngine.Execute(@"aelf.chain.connectChain()");

            #endregion


            string mnenomic = "degree family lab shrimp such demand stay popular inflict immune attack mouse";
            string privKey = "69fbc2a6020e93e8b1390505d89434d5be3d60e04dcfc48b4c02854619a8c93d";
            string address = "648043cd7ceea25ba5c7a2b4e3789064cec3";
            var obj = jsEngine.Evaluate($@"Aelf.wallet.getWalletByMnemonic(""{mnenomic}"")");

            Assert.Equal(mnenomic, obj.ReadProperty<string>("mnemonic"));
            Assert.Equal(privKey, obj.ReadProperty<string>("privateKey"));
            Assert.Equal(address, obj.ReadProperty<string>("address"));
        }

//        [Fact]
//        public void TestXMLHttpRequest()
//        {
//            var jsEngine = GetJSEngine();
//            jsEngine.RunScript(@"
//                var request = new XMLHttpRequest()
//                request.open(""GET"", ""http://www.baidu.com"")
//            ");
//            Assert.Equal(jsEngine.Get("request").Get("readyState").Value.ToInt32(), 1);
//        }
    }
}