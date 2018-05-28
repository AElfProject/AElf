using System;
using AElf.Database.SsdbClient;
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

            _database.SetAsync(key, Helper.StringToBytes((value)));
        }
        
        [Fact]
        public void GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, Helper.StringToBytes((value)));
            var getResult = _database.GetAsync(key, null);
            
            Assert.Equal(value,Helper.BytesToString(getResult.Result));
        }
    }
}