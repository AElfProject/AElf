using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf.Database.Client;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Database.Tests
{
    [UseAutofacTestFramework]
    public class SsdbDatebaseTest
    {
        private readonly IKeyValueDatabase _database;

        public SsdbDatebaseTest()
        {
            _database = new SsdbDatebase();
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

            _database.SetAsync(key, ConvertToBytes((value)));
            var getResult = _database.GetAsync(key, null);
            var getResultStr = ConvertToString(getResult.Result);
            
            Assert.Equal(value,getResultStr);
        }
        
        private byte[] ConvertToBytes(string s)	{
            return Encoding.Default.GetBytes(s);
        }

        private string ConvertToString(byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }
    }
}