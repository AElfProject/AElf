using System;
using AElf.Database.SsdbClient;
using Xunit;

namespace AElf.Database.Tests
{
    public class SsdbLinkTest:IDisposable
    {
        private static readonly string _host = "127.0.0.1";
        private static readonly int _port = 8888;
        private readonly Link _link;

        public SsdbLinkTest()
        {
            _link = new Link(_host,_port);
            _link.Connect();
        }

        [Fact]
        public void ConnectTest()
        {
            var result = _link.Connect();
            Assert.True(result);
        }

        [Fact]
        public void RequestTest()
        {
            var key = "unittestrequest";
            var value = Guid.NewGuid().ToString();

            var setResult = _link.Request(Command.Set, new string[] {key, value});
            Assert.True(Helper.BytesEqual(setResult[0], ResponseCode.OkByte));

            var getResult = _link.Request(Command.Get, new string[] {key});
            Assert.True(getResult.Count == 2);
            Assert.True(Helper.BytesEqual(getResult[0], ResponseCode.OkByte));
            Assert.Equal(Helper.BytesToString(getResult[1]), value);

            var delResult = _link.Request(Command.Del, new string[] {key});
            Assert.True(Helper.BytesEqual(delResult[0], ResponseCode.OkByte));

            var existsResult = _link.Request(Command.Exists, new string[] {key});
            Assert.True(Helper.BytesToString(existsResult[0]) == ResponseCode.NotFound ||
                        Helper.BytesToString(existsResult[1]) == "0");
        }

        public void Dispose()
        {
            _link.Close();
        }
    }
}