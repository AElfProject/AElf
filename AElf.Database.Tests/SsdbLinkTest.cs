using System;
using AElf.Database.SsdbClient;
using Xunit;

namespace AElf.Database.Tests
{
    public class SsdbLinkTest
    {
        private static readonly string _host = "127.0.0.1";

        private static readonly int _port = 8888;

        [Fact]
        public void ConnectTest()
        {
            using (var link = new Link(_host, _port))
            {
                var result = link.Connect();
                Assert.True(result);
            }
        }

        [Fact]
        public void RequestTest()
        {
            var key = "unittestrequest";
            var value = Guid.NewGuid().ToString();
            using (var link = new Link(_host, _port))
            {
                link.Connect();

                var setResult = link.Request(Command.Set, new string[] {key, value});
                Assert.True(Helper.BytesEqual(setResult[0],ResponseCode.OkByte));

                var getResult = link.Request(Command.Get, new string[] {key});
                Assert.True(getResult.Count==2);
                Assert.True(Helper.BytesEqual(getResult[0],ResponseCode.OkByte));
                Assert.Equal(Helper.BytesToString(getResult[1]),value);

                var delResult = link.Request(Command.Del, new string[] {key});
                Assert.True(Helper.BytesEqual(delResult[0], ResponseCode.OkByte));
                
                var existsResult = link.Request(Command.Exists, new string[] {key});
                Assert.True(Helper.BytesToString(existsResult[0])==ResponseCode.NotFound || Helper.BytesToString(existsResult[1]) == "0" );
            }
        }
    }
}