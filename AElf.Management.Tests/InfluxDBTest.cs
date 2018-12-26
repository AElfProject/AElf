using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Helper;
using Xunit;

namespace AElf.Management.Tests
{
    public class InfluxDBTest
    {
        [Fact(Skip = "require InfluxDB")]
        // [Fact]
        public async Task TestSetAndGet()
        {
            var database = "unittest";
            await InfluxDBHelper.CreateDatabase(database);

            var used = 50;
            var time = DateTime.Now;
            await InfluxDBHelper.Set(database, "cpu", new Dictionary<string, object> {{"used", used}}, null, time);
            Thread.Sleep(1000);
            var result = await InfluxDBHelper.Get(database, "select * from cpu");

            Assert.True(Convert.ToInt32(result[0].Values[0][1]) == used);

            await InfluxDBHelper.DropDatabase(database);
        }

        [Fact(Skip = "require InfluxDB")]
        // [Fact]
        public async Task TestVerison()
        {
            var version = await InfluxDBHelper.Version();
            Assert.NotNull(version);
        }
    }
}