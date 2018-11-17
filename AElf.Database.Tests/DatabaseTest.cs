using System;
using Xunit;

namespace AElf.Database.Tests
{
    public class DatabaseTest
    {
        private readonly IKeyValueDatabase _database;

        public DatabaseTest()
        {
            _database = new InMemoryDatabase();
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

            _database.SetAsync("Default",key, Helper.StringToBytes(value));
        }

        [Fact]
        public void GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync("Default",key, Helper.StringToBytes(value));
            var getResult = _database.GetAsync("Default",key);

            Assert.Equal(value, Helper.BytesToString(getResult.Result));
        }
    }
}