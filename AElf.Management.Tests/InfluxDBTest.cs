using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Database;
using AElf.Management.Helper;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.Management.Tests
{
    public class InfluxDBTest : ManagementTestBase
    {
        private readonly IInfluxDatabase _influxDatabase;
        
        public InfluxDBTest()
        {
            _influxDatabase = GetRequiredService<IInfluxDatabase>();
        }

        [Fact(Skip = "require InfluxDB")]
        // [Fact]
        public async Task TestSetAndGet()
        {
            var database = "unittest";
            await _influxDatabase.CreateDatabase(database);

            var used = 50;
            var time = DateTime.Now;
            await _influxDatabase.Set(database, "cpu", new Dictionary<string, object> {{"used", used}}, null, time);
            Thread.Sleep(1000);
            var result = await _influxDatabase.Get(database, "select * from cpu");

            Assert.True(Convert.ToInt32(result[0].Values[0][1]) == used);

            await _influxDatabase.DropDatabase(database);
        }

        [Fact(Skip = "require InfluxDB")]
        // [Fact]
        public async Task TestVerison()
        {
            var version = await _influxDatabase.Version();
            Assert.NotNull(version);
        }
    }
}