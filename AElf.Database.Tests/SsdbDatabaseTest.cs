using System;
using System.Linq;
using AElf.Database.SsdbClient;
using AElf.Kernel;
using ServiceStack;
using Xunit;

namespace AElf.Database.Tests
{
    public class SsdbDatabaseTest
    {
        private readonly IKeyValueDatabase _database;

        public SsdbDatabaseTest()
        {
            _database = new SsdbDatabase();
        }

        [Fact]
        public void IsConnectedTest()
        {
            var result = _database.IsConnected();
            Assert.True(result);
        }
        
        [Fact]
        public void SetTest()
        {
            var key = "settest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, new Hash(Helper.StringToBytes(value)));
        }
        
        [Fact]
        public void GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, new Hash(Helper.StringToBytes(value)));
            var getResult = _database.GetAsync(key, null);
            
            Assert.Equal(value, Helper.BytesToString(getResult.Result));
        }
    }
}