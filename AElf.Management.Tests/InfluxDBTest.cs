using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Management.Helper;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class InfluxDBTest
    {
        [Fact]
        public void TestSetAndGet()
        {
            var database = "unittest";
            InfluxDBHelper.CreateDatabase(database);

            var used = 50;
            var time = DateTime.Now;
            InfluxDBHelper.Set(database, "cpu", new Dictionary<string, object> {{"used", used}}, null, time);
            Thread.Sleep(1000);
            var result = InfluxDBHelper.Get(database, "select * from cpu");
            
            Assert.True(Convert.ToInt32(result[0].Values[0][1]) == used);
            
            InfluxDBHelper.DropDatabase(database);
        }
        
        [Fact]
        public void TestVerison()
        {
            var version = InfluxDBHelper.Version();
            Assert.NotNull(version);
        }
    }
}