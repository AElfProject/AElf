using System;
using StackExchange.Redis;
using Xunit;

namespace AElf.Database.Tests
{
    public class StackExchangeTest
    {
        private readonly ConfigurationOptions _options;
        private readonly string _host = "127.0.0.1";
        private readonly int _port = 8888;
        
        public StackExchangeTest()
        {
            _options=new ConfigurationOptions
            {
                EndPoints = {{_host, _port}},
                CommandMap = CommandMap.SSDB
            };
            
        }

//        [Fact]
//        public void ClearDB()
//        {
//            var adminOptions = new ConfigurationOptions
//            {
//                EndPoints = {{_host, _port}},
//                CommandMap = _options.CommandMap,
//                AllowAdmin = true,
//            };
//            
//            using (var conn = ConnectionMultiplexer.Connect(adminOptions))
//            {
//                var server = conn.GetServer(conn.GetEndPoints().First());
//                try
//                {
//                    var keys = server.Keys();
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine(e);
//                    throw;
//                }
//            }
//        }

        [Fact]
        public void PingTest()
        {
            using (var conn = ConnectionMultiplexer.Connect(_options))
            {
                var db = conn.GetDatabase(0);
                db.Ping();
            }
        }
        
        [Fact]
        public void SetAsyncTest()
        {
            var key = "SetAsyncTest";
            var value = Guid.NewGuid().ToString();
            using (var conn = ConnectionMultiplexer.Connect(_options))
            {
                var db = conn.GetDatabase(0);
                var result = db.StringSetAsync(key, Helper.StringToBytes(value));
                Assert.True(result.Result);
            }
        }
        
        [Fact]
        public void GetAsyncTest()
        {
            var key = "GetAsyncTest";
            var value = Guid.NewGuid().ToString();
            using (var conn = ConnectionMultiplexer.Connect(_options))
            {
                var db = conn.GetDatabase(0);
                db.StringSetAsync(key, Helper.StringToBytes(value));
                
                var result = db.StringGetAsync(key);
                
                Assert.Equal(value,Helper.BytesToString(result.Result));
            }
        }
    }
}