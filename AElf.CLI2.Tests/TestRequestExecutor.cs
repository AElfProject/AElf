using System.IO;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using AElf.CLI2.Tests.Utils;
using Autofac;
using Xunit;
using Xunit.Abstractions;

namespace AElf.CLI2.Tests
{
    public class TestRequestExecutor
    {
        private IRequestExecutor GetRequestExecutor()
        {
            var option = new AccountOption
            {
                Endpoint = "",
                Password = "",
                Action = AccountAction.create,
                AccountFileName = "a.account"
            };
            return IoCContainerBuilder.Build(option).Resolve<IRequestExecutor>();
        }

        private readonly ITestOutputHelper _output;

        public TestRequestExecutor(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRequestLocalFile()
        {
            var requestExecutor = GetRequestExecutor();
            Assert.NotNull(requestExecutor);
            var fn = Path.GetTempFileName();
            using (var writer = new StreamWriter(File.OpenWrite(fn)))
            {
                writer.Write("abc");
            }

            var resp = requestExecutor.ExecuteAsync("GET", $"file://{fn}", null, string.Empty).Result;
            var fileResp = resp as LocalFileResponse;
            Assert.NotNull(fileResp);
            Assert.Equal(fileResp.Content, "abc");
            File.Delete(fn);
        }

        [Fact]
        public void TestHttpGet()
        {
            var requestExecutor = GetRequestExecutor();
            Assert.NotNull(requestExecutor);
            var resp = requestExecutor.ExecuteAsync("GET", "http://www.baidu.com", null, string.Empty);
            resp.Wait();
            var httpResp = resp.Result as HttpResponse;
            Assert.NotNull(httpResp);
        }
    }
}