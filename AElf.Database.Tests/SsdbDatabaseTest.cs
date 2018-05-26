using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf.Database.SsdbClient;
using Xunit;
using Xunit.Frameworks.Autofac;

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
        public void SetAndGetTest()
        {
            var key = "UintTest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, Helper.StringToBytes((value)));
            var getResult = _database.GetAsync(key, null);
            var getResultStr = Helper.BytesToString(getResult.Result);
            
            Assert.Equal(value,getResultStr);
        }
    }
}