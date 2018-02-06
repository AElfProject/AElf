using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionSchedulerTest
    {
        private List<byte[]> CreateAccountAddressList(int accountCount)
        {
            List<byte[]> accountAddressList = new List<byte[]>();
            
            for (int j = 0; j < accountCount; j++)
            {
                accountAddressList.Add(new []{(byte)(j + 'a')});
            }
            return accountAddressList;
        }
        
        
        private ITransaction CreateTransaction(byte b, byte[] from, byte[] to)
        {
            Mock<IHash<ITransaction>> hash = new Mock<IHash<ITransaction>>();
            hash.Setup(h => h.GetHashBytes()).Returns(new []{b});
            hash.Setup(h => h.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes() == t.GetHashBytes() );
            
            Mock <ITransaction> transaction=new Mock<ITransaction>();
            transaction.Setup(t => t.GetHash()).Returns(hash.Object);
            transaction.Setup(t => t.From).Returns(from);
            transaction.Setup(t => t.To).Returns(to);

            Mock.Get(transaction.Object).Setup(m => m.Equals(It.IsAny<ITransaction>()))
                .Returns<ITransaction>(t =>t?.GetHash().GetHashBytes() == transaction.Object.GetHash().GetHashBytes());
            
            return transaction.Object;
        }

        
        [Fact]
        public void SchedulerTest()
        {
            WorldState worldState = new WorldState();
            var transactionExecutingManager = new TransactionExecutingManager(worldState);
            
            // simple demo cases


            var accountAddressList = CreateAccountAddressList(10);
            // one tx
            // A
            var tx1 = CreateTransaction((byte) 'A', accountAddressList[0], accountAddressList[1]);
            
            var transactions = new List<ITransaction> {tx1};
            
            transactionExecutingManager.Schedule(transactions);

            var plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(1, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            
            
            // two txs
            // two seperate txs
            // A B
            var tx2 = CreateTransaction((byte) 'B', accountAddressList[2], accountAddressList[3] );
            transactions = new List<ITransaction>{tx1, tx2};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(1, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            
          


            // two connected txs
            // A-B
            tx2 = CreateTransaction((byte) 'B', accountAddressList[0], accountAddressList[1] );
            transactions = new List<ITransaction>{tx1, tx2};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            
            
            
            // three txs
            
            
            // two connected and one more seperate
            // A-B C
            var tx3 = CreateTransaction((byte) 'C', accountAddressList[2], accountAddressList[3]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
           
            // one connected with the other two
            // A-B B-C
            tx2 = CreateTransaction((byte) 'B', accountAddressList[1], accountAddressList[2]);
            tx3 = CreateTransaction((byte) 'C', accountAddressList[2], accountAddressList[3]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            
            // three txs connected with each other, three edges
            // A-B B-C C-A
            
            tx3 = CreateTransaction((byte) 'C', accountAddressList[0], accountAddressList[2]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(3, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            

            
            // four txs
            
            // one pair of txs, and the other two seperated, one edge
            // A-B C D
            tx3 = CreateTransaction((byte) 'C', accountAddressList[3], accountAddressList[4]);
            var tx4 = CreateTransaction((byte) 'D', accountAddressList[5], accountAddressList[6]);
            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[0].ElementAt(2).GetHash().GetHashBytes()[0]);
            
            
            
            // two pairs of txs, two edges
            // A-B C-D
            tx3 = CreateTransaction((byte) 'C', accountAddressList[3], accountAddressList[4]);
            tx4 = CreateTransaction((byte) 'D', accountAddressList[4], accountAddressList[5]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            // one tx connected with other three, three edges
            // A-B A-C A-D
            
            
            
            // one tx connected with other three and the other two connected with each other
            // 4 edsges
            // A-B A-C A-D C-D
            
            
            
            // 4 edges
            // A-B B-C C-D D-A
            tx2 = CreateTransaction((byte) 'B', accountAddressList[1], accountAddressList[2]);
            tx3 = CreateTransaction((byte) 'C', accountAddressList[2], accountAddressList[3]);
            tx4 = CreateTransaction((byte) 'D', accountAddressList[3], accountAddressList[0]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            // one tx connected with other three
            // one tx connected with the other two
            // 5 edsges
            // A-B A-C A-D C-D C-B 
            tx2 = CreateTransaction((byte) 'B', accountAddressList[1], accountAddressList[2]);
            tx3 = CreateTransaction((byte) 'C', accountAddressList[1], accountAddressList[3]);
            tx4 = CreateTransaction((byte) 'D', accountAddressList[3], accountAddressList[0]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(3, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            // connect each other, 6 edsges
            // A-B A-C A-D B-C B-D C-D 
            tx2 = CreateTransaction((byte) 'B', accountAddressList[0], accountAddressList[1]);
            tx3 = CreateTransaction((byte) 'C', accountAddressList[0], accountAddressList[1]);
            tx4 = CreateTransaction((byte) 'D', accountAddressList[0], accountAddressList[1]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(4, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[3].ElementAt(0).GetHash().GetHashBytes()[0]);
        }
        
    }
}