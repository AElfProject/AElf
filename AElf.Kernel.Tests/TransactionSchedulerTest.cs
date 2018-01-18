using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuickGraph;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionSchedulerTest
    {
        
        private static IHash CreateHash(byte b)
        {
            Mock<IHash> hash = new Mock<IHash>();
            hash.Setup(h => h.GetHashBytes()).Returns(new byte[]{b});
            hash.Setup(h => h.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes() == t.GetHashBytes());

            return hash.Object;
        }
        
        
        private static List<IHash> CreateIHashList(int hashCount)
        {
            List<IHash> hashList=new List<IHash>();
            int j = 0;
            while (hashCount > j++)
            {
                hashList.Add(CreateHash((byte)j));
            }
            
            return hashList;
        }
        
        
        private static ITransaction CreateTransaction(byte b)
        {
            Mock<IHash<ITransaction>> hash=new Mock<IHash<ITransaction>>();
            hash.Setup(h => h.GetHashBytes()).Returns(new byte[]{b});
            hash.Setup(h => h.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes() == t.GetHashBytes() );
            
            Mock <ITransaction> transaction=new Mock<ITransaction>();
            transaction.Setup(t => t.GetHash()).Returns(hash.Object);

            return transaction.Object;
        }

        
        private static Dictionary<IHash, List<ITransaction>> CreatePending(int hashCount, int txCount)
        {
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            List<IHash> hashList = CreateIHashList(hashCount);
            
            // create vertices 
            byte i = (byte)'A';
            int k = 0;
            
            while (k++ < txCount)
            {
                var t = CreateTransaction(i++);
                Random random = new Random();

                do
                {
                    int index = random.Next(hashCount);
                    if(index>=hashCount) continue;
                    var h = hashList.ElementAt(index);
                    if (!pending.Keys.Contains(h)) pending[h] = new List<ITransaction>();
                    if(!pending[h].Contains(t)) pending[h].Add(t);
                } 
                while (random.Next(hashCount) < hashCount / 2);
            }

            return pending;
        }

        public void AsyncExecuteTest()
        {
            
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            
            var transactionExecutingManager = new TransactionExecutingManager { Pending = pending };

            // 5 hash and 10 tx
            int hashCount = 5;
            int txCount = 10;
            
            pending = CreatePending(hashCount, txCount);
            transactionExecutingManager.Pending = pending;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            transactionExecutingManager.Schedule();
            stopwatch.Stop();
            
        }
        
        
        
        public void CalculateExecutingPlanTest()
        {
            
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            var transactionExecutingManager = new TransactionExecutingManager { Pending = pending };
            
            // 5 hash and 10 tx
            int hashCount = 5;
            int txCount = 10;
            
            pending = CreatePending(hashCount, txCount);
            transactionExecutingManager.Pending = pending;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            transactionExecutingManager.Schedule();
            stopwatch.Stop();
            
           
            var plan = TransactionExecutingManager.ExecutePlan;
            int count = 0;
            foreach (var p in plan)
            {
                count += p.Value.Count;
            }
            Assert.Equal(txCount, count);
            
        }


        [Fact]
        public void ColorGraphTest()
        {
            
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
       
            var transactionExecutingManager = new TransactionExecutingManager { Pending = pending };
            
            // two hash and five tx
            int hashCount = 1000;
            int txCount = 1000;
            pending = CreatePending(hashCount, txCount);
            transactionExecutingManager.Pending = pending;
            Stopwatch stopwatch = new Stopwatch();
            
            stopwatch.Start();
            transactionExecutingManager.Schedule();
         
        }
        
        
        
    }
}