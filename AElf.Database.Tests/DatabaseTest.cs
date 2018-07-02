using System;
using Xunit;

namespace AElf.Database.Tests
{
    public class DatabaseTest
    {
        private readonly IKeyValueDatabase _database;

        public DatabaseTest()
        {
            _database = new KeyValueDatabase();
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

            _database.SetAsync(key, Helper.StringToBytes(value));
        }

        [Fact]
        public void GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, Helper.StringToBytes(value));
            var getResult = _database.GetAsync(key, null);

            Assert.Equal(value, Helper.BytesToString(getResult.Result));
        }
    }
}