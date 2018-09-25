using System;
using System.Threading;
using AElf.Management.Database;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class InfluxDBTest
    {
        [Fact]
        public void TestSet()
        {
            //InfluxDBHelper.Set("");
        }
        
        [Fact]
        public void TestGet()
        {
            InfluxDBHelper.Get("","");
        }
        
        [Fact]
        public void TestVerison()
        {
            InfluxDBHelper.Version();
        }

        [Fact]
        public void Test()
        {
            try
            {
                var service = new TransactionService();

                service.RecordPoolSize("test", DateTime.Now, 234);

                while (true)
                {
                    Thread.Sleep(3000);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}