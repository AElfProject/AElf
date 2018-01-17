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
        public static Dictionary<IHash, int> ExecutePlan = new Dictionary<IHash, int>();
        
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
        
        
        [Fact]
        public void ScheduleTest()
        {
            var h1 = CreateHash(1);
            var t1 = CreateTransaction(65);

            // one hash and one tx
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            pending[h1]=new List<ITransaction>();
            pending[h1].Add(t1);
            var transactionExecutingManager = new TransactionExecutingManager { Pending = pending };
            transactionExecutingManager.Schedule();
            var plan = TransactionExecutingManager.ExecutePlan;
            Assert.Equal(1, plan[0].Count);

            // one hash and two tx
            transactionExecutingManager.Pending = CreatePending(1,3);
            transactionExecutingManager.Schedule();
            plan = TransactionExecutingManager.ExecutePlan;
            Assert.Equal(3, plan.Count);
            Assert.Equal(1, plan[0].Count);
            Assert.Equal(1, plan[1].Count);
            
            
            // two hash and five tx
            pending = CreatePending(2, 5);
            transactionExecutingManager.Pending = pending;
            transactionExecutingManager.Schedule();
            plan = TransactionExecutingManager.ExecutePlan;
            Assert.Equal(5, plan.Count);

        }
        
    }
}