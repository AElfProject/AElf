using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Kernel.KernelAccount;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionSchedulerTest
    {
        
        private Mock<Hash> CreateHash(byte b)
        {
            Mock<Hash> hash = new Mock<Hash>();
            hash.Setup(h => h.GetHashBytes()).Returns(new[] {b});
            
            Mock.Get(hash.Object).Setup(h => h.Equals(It.IsAny<Hash>()))
                .Returns<Hash>(t => t?.GetHashBytes() == hash.Object.GetHashBytes());
            return hash;
        }
        
        
        private IAccount CreateAccount(byte b)
        {
            var hash = CreateHash(b);
            
            
            Mock <IAccount> account=new Mock<IAccount>();
            account.Setup(a => a.GetAddress()).Returns( hash.Object );
           
            Mock.Get(account.Object).Setup(a => a.Equals(It.IsAny<ITransaction>()))
                .Returns<IAccount>(t =>t?.GetAddress().GetHashBytes() == account.Object.GetAddress().GetHashBytes());
            return account.Object;
        }
        
        private List<Hash> CreateHashList(int accountCount)
        {
            List<Hash> hashes = new List<Hash>();
            for (int j = 0; j < accountCount; j++)
            {
                hashes.Add(CreateHash((byte)(j + 'a')).Object);
            }
            return hashes;
        }
        
        
        private ITransaction CreateTransaction(byte b, Hash from, Hash to)
        {
            Mock<Hash> hash = new Mock<Hash>();
            hash.Setup(h => h.GetHashBytes()).Returns(new []{b});
            hash.Setup(h => h.Equals(It.IsAny<Hash>()))
                .Returns<Hash>(t => t? .GetHashBytes() == t.GetHashBytes() );
            
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
            
            ISmartContractService sm = null;
            IChainContext context = null;
            var transactionExecutingManager = new TransactionExecutingService(sm);
            
            // simple demo cases

            var accounts = CreateHashList(10);
            // one tx
            // A
            var tx1 = CreateTransaction((byte) 'A', accounts[0], accounts[1]);
            
            var transactions = new List<ITransaction> {tx1};
            
            transactionExecutingManager.Schedule(transactions, context);

            var plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(1, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            
            
            // two txs
            // two seperate txs
            // A B
            var tx2 = CreateTransaction((byte) 'B', accounts[2], accounts[3] );
            transactions = new List<ITransaction>{tx1, tx2};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(1, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            
          


            // two connected txs
            // A-B
            tx2 = CreateTransaction((byte) 'B', accounts[0], accounts[1] );
            transactions = new List<ITransaction>{tx1, tx2};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            
            
            
            
            // three txs
            
            
            // two connected and one more seperate
            // A-B C
            var tx3 = CreateTransaction((byte) 'C', accounts[2], accounts[3]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
           
            // one connected with the other two
            // A-B B-C
            tx2 = CreateTransaction((byte) 'B', accounts[1], accounts[2]);
            tx3 = CreateTransaction((byte) 'C', accounts[2], accounts[3]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            
            // three txs connected with each other, three edges
            // A-B B-C C-A
            
            tx3 = CreateTransaction((byte) 'C', accounts[0], accounts[2]);
            transactions = new List<ITransaction>{tx1, tx2, tx3};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(3, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            

            
            // four txs
            
            // one pair of txs, and the other two seperated, one edge
            // A-B C D
            tx3 = CreateTransaction((byte) 'C', accounts[3], accounts[4]);
            var tx4 = CreateTransaction((byte) 'D', accounts[5], accounts[6]);
            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[0].ElementAt(2).GetHash().GetHashBytes()[0]);
            
            
            
            // two pairs of txs, two edges
            // A-B C-D
            tx3 = CreateTransaction((byte) 'C', accounts[3], accounts[4]);
            tx4 = CreateTransaction((byte) 'D', accounts[4], accounts[5]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions, context);
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
            tx2 = CreateTransaction((byte) 'B', accounts[1], accounts[2]);
            tx3 = CreateTransaction((byte) 'C', accounts[2], accounts[3]);
            tx4 = CreateTransaction((byte) 'D', accounts[3], accounts[0]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions, context);
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
            tx2 = CreateTransaction((byte) 'B', accounts[1], accounts[2]);
            tx3 = CreateTransaction((byte) 'C', accounts[1], accounts[3]);
            tx4 = CreateTransaction((byte) 'D', accounts[3], accounts[0]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(3, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(1).GetHash().GetHashBytes()[0]);
            
            
            // connect each other, 6 edsges
            // A-B A-C A-D B-C B-D C-D 
            tx2 = CreateTransaction((byte) 'B', accounts[0], accounts[1]);
            tx3 = CreateTransaction((byte) 'C', accounts[0], accounts[1]);
            tx4 = CreateTransaction((byte) 'D', accounts[0], accounts[1]);

            transactions = new List<ITransaction>{tx1, tx2, tx3, tx4};
            transactionExecutingManager.Schedule(transactions, context);
            plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(4, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHash().GetHashBytes()[0]);
            Assert.Equal(68, plan[3].ElementAt(0).GetHash().GetHashBytes()[0]);
            
        }

        
    }
}
