using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.Token;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Parallel.MultiToken.Tests
{
    public class TokenTransactionGrouperTest : ParallelMultiTokenTestBase
    {
        private readonly ITransactionGrouper _grouper;  
        private OSTestHelper _osTestHelper;
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly Address _tokenAddress;
        
        public TokenTransactionGrouperTest()
        {
            _grouper = GetRequiredService<ITransactionGrouper>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            
            _tokenAddress = _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        public async Task GroupTransaction(int group)
        {
            var transactions = GenerateGroupTransactions(group);
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            groups.Count.ShouldBe(group);
            nonParallelizables.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GroupTransaction_SameOwner()
        {
            var from = Address.Generate();
            var to1 = Address.Generate();
            var to2 = Address.Generate();
            
            var transactions = new List<Transaction>
            {
                GenerateTransferTransaction(from, to1, 10),
                GenerateTransferTransaction(from, to2, 10),
            };
            
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            groups.Count.ShouldBe(1);
            nonParallelizables.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GroupTransaction_SameReceiver()
        {
            var from1 = Address.Generate();
            var from2 = Address.Generate();

            var to = Address.Generate();
            
            var transactions = new List<Transaction>
            {
                GenerateTransferTransaction(from1, to, 10),
                GenerateTransferTransaction(from2, to, 10),
            };
            
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            
            groups.Count.ShouldBe(1);
            groups.First().Count.ShouldBe(2);
            
            nonParallelizables.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task GroupTransaction_DifferentOwner()
        {
            var accountA = Address.Generate();
            var accountB = Address.Generate();
            var accountC = Address.Generate();
            
            var transactions = new List<Transaction>
            {
                GenerateTransferTransaction(accountA, accountB, 10),
                GenerateTransferTransaction(accountB, accountC, 10),
            };
            
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            
            groups.Count.ShouldBe(1);
            groups.First().Count.ShouldBe(2);
            
            nonParallelizables.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task GroupTransaction_DifferentOwner_DifferentReceiver()
        {
            var accountA = Address.Generate();
            var accountB = Address.Generate();
            var accountC = Address.Generate();
            var accountD = Address.Generate();
            
            var transactions = new List<Transaction>
            {
                GenerateTransferTransaction(accountA, accountB, 10),
                GenerateTransferTransaction(accountC, accountD, 10),
            };
            
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            groups.Count.ShouldBe(2);
            nonParallelizables.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GroupTransaction_SameOwner_Different_Method()
        {
            var from = Address.Generate();
            var to = Address.Generate();
            
            var transactions = new List<Transaction>
            {
                GenerateTransferTransaction(from, to, 10),
                GenerateTransferFromTransaction(from, to, 10)
            };
            
            var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
            groups.Count.ShouldBe(1);
            nonParallelizables.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GroupTransaction_ComplexScenario()
        {
            var accountA = Address.Generate();
            var accountB = Address.Generate();
            var accountC = Address.Generate();
            var accountD = Address.Generate();
            var accountE = Address.Generate();
            var accountF = Address.Generate();
            
            List<Transaction> transactions;
            // no conflict A->B, C->D, E->F
            {
                transactions = new List<Transaction>
                {
                    GenerateTransferTransaction(accountA, accountB, 100),
                    GenerateTransferTransaction(accountC, accountD, 100),
                    GenerateTransferTransaction(accountE, accountF, 100)
                };
                
                var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
                groups.Count.ShouldBe(3);
                nonParallelizables.Count.ShouldBe(0);
            }
            
            //add B->C tx
            {
                transactions.Add(GenerateTransferTransaction(accountB, accountC, 100));
                
                var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
                groups.Count.ShouldBe(2);
                nonParallelizables.Count.ShouldBe(0);
            }
            
            //add D->E tx
            {
                transactions.Add(GenerateTransferTransaction(accountD, accountE, 100));
                
                var (groups, nonParallelizables) = await _grouper.GroupAsync(transactions);
                groups.Count.ShouldBe(1);
                nonParallelizables.Count.ShouldBe(0);
            }
        }
        
        private List<Transaction> GenerateGroupTransactions(int group, int count = 3)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < group; i++)
            {
                var from = Address.Generate();
                for (var j = 0; j < count; j++)
                {
                    var to = Address.Generate();
                    var tx = GenerateTransferTransaction(from, to, 10);
                    
                    transactions.Add(tx);
                }
            }

            return transactions;
        }

        private Transaction GenerateTransferTransaction(Address from, Address to, long amount)
        {
            return GenerateTokenTransaction(from,"Transfer", new TransferInput
            {
                Amount = amount,
                Symbol = "ELF",
                To = to,
                Memo = "Token transfer"
            });
        }
        
        private Transaction GenerateTransferFromTransaction(Address from, Address to, long amount)
        {
            return GenerateTokenTransaction(from,"TransferFrom", new TransferFromInput
            {
                Amount = amount,
                Symbol = "ELF",
                From = from,
                To = to,
                Memo = "Transfer from transaction"
            });
        }

        private Transaction GenerateTokenTransaction(Address from, string method, IMessage parameters)
        {
            var tx = _osTestHelper.GenerateTransaction(from, _tokenAddress, method, parameters);

            return tx;
        }
    }
}