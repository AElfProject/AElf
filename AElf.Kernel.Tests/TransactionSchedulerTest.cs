using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionSchedulerTest
    {
        
        private static IHash CreateHash(byte b)
        {
            Mock<IHash> hash = new Mock<IHash>();
            hash.Setup(h => h.GetHashBytes()).Returns(new[]{b});
            hash.Setup(h => h.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes() == t.GetHashBytes());

            return hash.Object;
        }
        
        
        private static List<IHash> CreateIHashList(int hashCount)
        {
            List<IHash> hashList=new List<IHash>();
            int j = 0;
            while (hashCount > j)
            {
                hashList.Add(CreateHash((byte)(j++ + 'a')));
            }
            
            return hashList;
        }
        
        
        private static ITransaction CreateTransaction(byte b)
        {
            Mock<IHash<ITransaction>> hash=new Mock<IHash<ITransaction>>();
            hash.Setup(h => h.GetHashBytes()).Returns(new []{b});
            hash.Setup(h => h.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes() == t.GetHashBytes() );
            
            Mock <ITransaction> transaction=new Mock<ITransaction>();
            transaction.Setup(t => t.GetHash()).Returns(hash.Object);

            return transaction.Object;
        }

        
        private static Dictionary<IHash, List<ITransaction>> CreatePending(int hashCount, int txCount, int hashOccupiedCount)
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

                int u = 0;
                while(u++ < hashOccupiedCount)
                {
                    int index = random.Next(hashCount);
                    var h = hashList.ElementAt(index);
                    if (!pending.Keys.Contains(h)) pending[h] = new List<ITransaction>();
                    if(!pending[h].Contains(t)) pending[h].Add(t);
                } 
            }

            return pending;
        }

        
        
        
        [Fact]
        public void SchedulerTest()
        {
            
            // simple demo cases
            var tx1 = CreateTransaction((byte) 'A');
            var tx2 = CreateTransaction((byte) 'B');
            var tx3 = CreateTransaction((byte) 'C');
            var tx4 = CreateTransaction((byte) 'D');

            
            List<IHash> hashes = CreateIHashList(10);


            Dictionary<IHash, List<ITransaction>> pending =
                new Dictionary<IHash, List<ITransaction>> {[hashes.ElementAt(0)] = new List<ITransaction> {tx1}};

            // one tx

            var transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();

            var plan = transactionExecutingManager.ExecutingPlan;
            Assert.Equal(1, plan.Count);
            Assert.Equal(plan[0].ElementAt(0).GetHashBytes()[0], 65);
            
            
            
            
            
            // two txs
            
            // two seperate txs
            // A B
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx2}
            };

            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(1, plan.Count);
            Assert.Equal(66, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[0].ElementAt(1).GetHashBytes()[0]);


            // two connected txs
            // A-B
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2}
            };


            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(66, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            
            
            
            
            // three txs
            
            
            // two connected and one more seperate
            // A-B C
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx3}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(66, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            
            
            // one connected with the other two
            // A-B B-C
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx2, tx3}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(66, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(1).GetHashBytes()[0]);
            
            
            // three txs connected with each other, three edges
            // A-B B-C C-A
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2, tx3}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(3, plan.Count);
            Assert.Equal(67, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[2].ElementAt(0).GetHashBytes()[0]);
            

            
            // four txs
            
            // one pair of txs, and the other two seperated, one edge
            // A-B C D
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx3},
                [hashes.ElementAt(2)] = new List<ITransaction> {tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(66, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[0].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(68, plan[0].ElementAt(2).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            
            
            
            // two pairs of txs, two edges
            // A-B C-D
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx3, tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(68, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[0].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[1].ElementAt(1).GetHashBytes()[0]);
            
            
            // one tx connected with other three, three edges
            // A-B A-C A-D
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx1, tx3},
                [hashes.ElementAt(2)] = new List<ITransaction> {tx1, tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[1].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(2).GetHashBytes()[0]);
            
            
            // one tx connected with other three and the other two connected with each other
            // 4 edsges
            // A-B A-C A-D C-D
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx1, tx3, tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(3, plan.Count);
            Assert.Equal(65, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(68, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[1].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(67, plan[2].ElementAt(0).GetHashBytes()[0]);
            
            
            // 4 edges
            // A-B B-C C-D D-A
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx2, tx3},
                [hashes.ElementAt(2)] = new List<ITransaction> {tx3, tx4},
                [hashes.ElementAt(3)] = new List<ITransaction> {tx4, tx1}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(2, plan.Count);
            Assert.Equal(68, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[0].ElementAt(1).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[1].ElementAt(1).GetHashBytes()[0]);
            
            
            // one tx connected with other three
            // one tx connected with the other two
            // 5 edsges
            // A-B A-C A-D C-D C-B 
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2, tx3},
                [hashes.ElementAt(1)] = new List<ITransaction> {tx1, tx3, tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(3, plan.Count);
            Assert.Equal(67, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[2].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(68, plan[2].ElementAt(1).GetHashBytes()[0]);
            
            
            // connect each other, 6 edsges
            // A-B A-C A-D B-C B-D C-D 
            pending = new Dictionary<IHash, List<ITransaction>>
            {
                [hashes.ElementAt(0)] = new List<ITransaction> {tx1, tx2, tx3, tx4}
            };
            
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;
            
            Assert.Equal(4, plan.Count);
            Assert.Equal(68, plan[0].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(65, plan[1].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(66, plan[2].ElementAt(0).GetHashBytes()[0]);
            Assert.Equal(67, plan[3].ElementAt(0).GetHashBytes()[0]);
            
            
            
            // cases for coloring algorithm, randolom created
            pending = CreatePending(50, 50, 2);
            transactionExecutingManager = new TransactionExecutingManager {Pending = pending};
            transactionExecutingManager.Schedule();
            plan = transactionExecutingManager.ExecutingPlan;

            Assert.True(!IsConfilt(plan, pending));
            
        }

        private static bool IsConfilt(Dictionary<int, List<IHash>> plan, Dictionary<IHash, List<ITransaction>> pending)
        {
            
            foreach (var phase in plan.Values)
            {
                foreach (var h1 in phase)
                {
                    foreach (var h2 in phase)
                    {
                        if (h1 == h2) continue;
                        foreach (var txs in pending.Values)
                        {
                            foreach (var t1 in txs)
                            {
                                if (t1.GetHash() == h1)
                                {
                                    if (txs.Any(t2 => t2.GetHash() == h2))
                                    {
                                        return true;
                                    }
                                }
                                if (t1.GetHash() != h2) continue;
                                {
                                    if (txs.Any(t2 => t2.GetHash() == h1))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return false;
             
        }


        private Dictionary<IHash, List<ITransaction>> RandomlyGeneratedCase()
        {
            const int hashCount = 500000;
            const int txCount = 5000;
            const int hashOccupiedCount = 2;

            var pending = CreatePending(hashCount, txCount, hashOccupiedCount);
            return pending;
        }

        
        
        private Dictionary<IHash, List<ITransaction>> SimpleDemoTestCases()
        {
            var tx1 = CreateTransaction((byte) 'A');
            var tx2 = CreateTransaction((byte) 'B');
            var tx3 = CreateTransaction((byte) 'C');

            var hashes = CreateIHashList(4);
            var pending =
                new Dictionary<IHash, List<ITransaction>>
                {
                    [hashes.ElementAt(0)] = new List<ITransaction> {tx1},
                    [hashes.ElementAt(1)] = new List<ITransaction> {tx1, tx3},
                    [hashes.ElementAt(2)] = new List<ITransaction> {tx2, tx3},
                    [hashes.ElementAt(3)] = new List<ITransaction> {tx2}
                };


            return pending;
            
        }
        
        
    }
}