using System;
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
        public void TestSet()
        {
            //InfluxDBHelper.Set("");
        }
        
        [Fact]
        public void TestGet()
        {
            var result = InfluxDBHelper.Get("0x2491b3fb14d2ddac790fc18c161166226f04","select * from node_state");
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