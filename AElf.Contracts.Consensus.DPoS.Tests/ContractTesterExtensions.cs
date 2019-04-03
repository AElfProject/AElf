using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// Extensions for consensus testing.
    /// </summary>
    public static class ContractTesterExtensions
    {
        #region IConsensusSmartContract

        public static async Task<ConsensusCommand> GetConsensusCommandAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            Timestamp timestamp)
        {
            var commandInput = new CommandInput
            {
                PublicKey = ByteString.CopyFrom(tester.KeyPair.PublicKey),
            };
            var bytes = await tester.CallContractMethodAsync(
                tester.GetConsensusContractAddress(), // Usually the second contract is consensus contract.
                ConsensusConsts.GetConsensusCommand,
                commandInput, timestamp.ToDateTime());
            return ConsensusCommand.Parser.ParseFrom(bytes);
        }

        public static async Task<DPoSHeaderInformation> GetInformationToUpdateConsensusAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation, DateTime dateTime)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GetInformationToUpdateConsensus, triggerInformation, dateTime);
            return DPoSHeaderInformation.Parser.ParseFrom(bytes);
        }

        public static async Task<List<Transaction>> GenerateConsensusTransactionsAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions, triggerInformation);
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            tester.SupplyTransactionParameters(ref txs);

            return txs;
        }

        public static async Task<ValidationResult> ValidateConsensusBeforeExecutionAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSHeaderInformation headerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.ValidateConsensusBeforeExecution, headerInformation);
            return ValidationResult.Parser.ParseFrom(bytes);
        }

        public static async Task<Block> GenerateConsensusTransactionsAndMineABlockAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation,
            params ContractTester<DPoSContractTestAElfModule>[] testersGonnaExecuteThisBlock)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions,
                triggerInformation);
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            tester.SupplyTransactionParameters(ref txs);

            var block = await tester.MineAsync(txs);
            foreach (var contractTester in testersGonnaExecuteThisBlock)
            {
                await contractTester.ExecuteBlock(block, txs);
            }

            return block;
        }

        #endregion

        #region Basic

        public static async Task<TransactionResult> ExecuteConsensusContractMethodWithMiningAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, IMessage input)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetConsensusContractAddress(),
                methodName, input);
        }

        public static async Task<(Block, Transaction)> ExecuteConsensusContractMethodWithMiningReturnBlockAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, IMessage input)
        {
            return await contractTester.ExecuteContractWithMiningReturnBlockAsync(
                contractTester.GetConsensusContractAddress(),
                methodName, input);
        }

        #endregion

        #region Handle Token

        public static Address GetTokenContractAddress(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }

        public static Address GetDividendsContractAddress(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(DividendsSmartContractAddressNameProvider.Name);
        }

        public static async Task<TransactionResult> ExecuteTokenContractMethodWithMiningAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, IMessage input)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                methodName, input);
        }

        public static async Task InitialChainAndTokenAsync(
            this ContractTester<DPoSContractTestAElfModule> starter, List<ECKeyPair> minersKeyPairs = null,
            int miningInterval = 4000, Timestamp blockchainStartTimestamp = null)
        {
            var dividendMethodCallList = new SystemTransactionMethodCallList();
            dividendMethodCallList.Add(nameof(DividendContract.InitializeDividendContract),
                new InitialDividendContractInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });

            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                Issuer = starter.GetCallOwnerAddress(),
                TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = "ELF",
                Amount = DPoSContractConsts.LockTokenForElection * 20,
                ToSystemContractName = DividendsSmartContractAddressNameProvider.Name,
                Memo = "Issue elf token."
            });

            // For testing.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = DPoSContractConsts.LockTokenForElection * 80,
                To = starter.GetCallOwnerAddress(),
                Memo = "Set dividends."
            });

            await starter.InitialCustomizedChainAsync(minersKeyPairs?.Select(m => m.PublicKey.ToHex()).ToList(),
                miningInterval, blockchainStartTimestamp,
                list =>
                {
                    // Dividends contract must be deployed before token contract.
                    list.AddGenesisSmartContract<DividendContract>(DividendsSmartContractAddressNameProvider.Name,
                        dividendMethodCallList);
                    list.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name,
                        tokenContractCallList);
                });
        }

        /// <summary>
        /// Unable to change round.
        /// </summary>
        /// <param name="miners"></param>
        /// <param name="blocksCount"></param>
        /// <param name="fakeTimestamp"></param>
        /// <returns></returns>
        public static async Task<ContractTester<DPoSContractTestAElfModule>> ProduceNormalBlocks(
            this List<ContractTester<DPoSContractTestAElfModule>> miners,
            int blocksCount, Timestamp fakeTimestamp = null)
        {
            var round = await miners.AnyOne().GetCurrentRoundInformationAsync();
            var startMiner = round.RealTimeMinersInformation.Values.OrderBy(v => v.Order)
                .FirstOrDefault(v => v.OutValue == null);

            // Terminate this method if blocks mining are complete in this round.
            if (startMiner == null)
            {
                return miners.First(m => m.PublicKey == round.RealTimeMinersInformation.Values
                                             .Last(v => v.Order == round.RealTimeMinersInformation.Count).PublicKey);
            }

            var startOrder = startMiner.Order;
            var endOrder = Math.Min(round.RealTimeMinersInformation.Count, startOrder + blocksCount - 1);
            var finalMinerPublicKey = "";
            for (var i = startOrder; i < endOrder + 1; i++)
            {
                var timestamp = DateTime.UtcNow.ToTimestamp();
                if (fakeTimestamp != null)
                {
                    timestamp = fakeTimestamp.ToDateTime().AddMilliseconds(1).ToTimestamp();
                }

                var currentMinerPublicKey = round.RealTimeMinersInformation.Values.First(v => v.Order == i).PublicKey;
                var currentMiner = miners.First(m => m.PublicKey == currentMinerPublicKey);
                var (block, tx) = await currentMiner.ExecuteConsensusContractMethodWithMiningReturnBlockAsync(
                    nameof(ConsensusContract.UpdateValue), new ToUpdate
                    {
                        OutValue = Hash.Generate(),
                        PreviousInValue = Hash.Generate(),
                        RoundId = round.RoundId,
                        Signature = Hash.Generate(),
                        PromiseTinyBlocks = 1,
                        ActualMiningTime = timestamp
                    });
                finalMinerPublicKey = currentMinerPublicKey;
                foreach (var otherMiner in miners.Where(m => m.PublicKey != currentMinerPublicKey))
                {
                    await otherMiner.ExecuteBlock(block, new List<Transaction> {tx});
                }
            }

            return miners.First(m =>
                m.PublicKey == round.RealTimeMinersInformation.Values.Last(v => v.PublicKey == finalMinerPublicKey)
                    .PublicKey);
        }

        /// <summary>
        /// Will use fake in value and out value.
        /// </summary>
        /// <param name="miners"></param>
        /// <param name="roundsCount"></param>
        /// <param name="changeTermFinally"></param>
        /// <returns></returns>
        public static async Task<ContractTester<DPoSContractTestAElfModule>> RunConsensusAsync(
            this List<ContractTester<DPoSContractTestAElfModule>> miners,
            int roundsCount, bool changeTermFinally = false)
        {
            var finalExtraBlockProducer = miners.First();
            for (var i = 0; i < roundsCount; i++)
            {
                var round = await miners.AnyOne().GetCurrentRoundInformationAsync();
                foreach (var miner in round.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
                {
                    var currentMiner = miners.First(m => m.PublicKey == miner.PublicKey);
                    var (block, tx) = await currentMiner.ExecuteConsensusContractMethodWithMiningReturnBlockAsync(
                        nameof(ConsensusContract.UpdateValue), new ToUpdate
                        {
                            OutValue = Hash.Generate(),
                            PreviousInValue = Hash.Generate(),
                            RoundId = round.RoundId,
                            Signature = Hash.Generate(),
                            PromiseTinyBlocks = 1,
                            ActualMiningTime = DateTime.UtcNow.ToTimestamp(),
                            ProducedBlocks = i + 1
                        });
                    foreach (var otherMiner in miners.Where(m => m.PublicKey != currentMiner.PublicKey))
                    {
                        await otherMiner.ExecuteBlock(block, new List<Transaction> {tx});
                    }
                }

                if (changeTermFinally && i == roundsCount - 1)
                {
                    finalExtraBlockProducer = await miners.ChangeTermAsync(round.GetMiningInterval());
                }
                else
                {
                    finalExtraBlockProducer = await miners.ChangeRoundAsync();
                }
            }

            return finalExtraBlockProducer;
        }

        public static async Task MineAsync(this List<ContractTester<DPoSContractTestAElfModule>> miners,
            List<Transaction> txs)
        {
            var block = await miners.First().MineAsync(txs);
            foreach (var miner in miners.Skip(1))
            {
                await miner.ExecuteBlock(block, txs);
            }
        }

        public static async Task<ContractTester<DPoSContractTestAElfModule>> ChangeRoundAsync(
            this List<ContractTester<DPoSContractTestAElfModule>> miners)
        {
            var round = await miners.AnyOne().GetCurrentRoundInformationAsync();

            var extraBlockProducer = round.GetExtraBlockProducerInformation();
            round.GenerateNextRoundInformation(DateTime.UtcNow, DateTime.UtcNow.ToTimestamp(),
                out var nextRound);
            var (extraBlock, extraTx) = await miners.First(m => m.PublicKey == extraBlockProducer.PublicKey)
                .ExecuteConsensusContractMethodWithMiningReturnBlockAsync(nameof(ConsensusContract.NextRound),
                    nextRound);

            foreach (var otherMiner in miners.Where(m => m.PublicKey != extraBlockProducer.PublicKey))
            {
                await otherMiner.ExecuteBlock(extraBlock, new List<Transaction> {extraTx});
            }

            return miners.First(m => m.PublicKey == extraBlockProducer.PublicKey);
        }

        /// <summary>
        /// Just change the term number and set dividends stuff, don't update miners list.
        /// </summary>
        /// <param name="miners"></param>
        /// <param name="miningInterval"></param>
        /// <returns></returns>
        /// <exception cref="Exception">Txs of changing term executing failed.</exception>
        public static async Task<ContractTester<DPoSContractTestAElfModule>> ChangeTermAsync(
            this List<ContractTester<DPoSContractTestAElfModule>> miners,
            int miningInterval)
        {
            var round = await miners.AnyOne().GetCurrentRoundInformationAsync();
            var currentTermNumber = await miners.AnyOne().GetCurrentTermNumber();

            var extraBlockProducer = round.GetExtraBlockProducerInformation();
            var nextRound = miners.Select(m => m.PublicKey).ToList().ToMiners()
                .GenerateFirstRoundOfNewTerm(miningInterval, DateTime.UtcNow, round.RoundNumber, currentTermNumber);

            var termNumberBytes = await miners.AnyOne().CallContractMethodAsync(
                miners.AnyOne().GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber),
                new Empty());
            var termNumber = SInt64Value.Parser.ParseFrom(termNumberBytes).Value;

            var nextTermTx = await miners.AnyOne().GenerateTransactionAsync(
                miners.AnyOne().GetConsensusContractAddress(),
                nameof(ConsensusContract.NextTerm), nextRound);

            var extraBlockMiner = miners.First(m => m.PublicKey == extraBlockProducer.PublicKey);
            var block = await extraBlockMiner.MineAsync(new List<Transaction> {nextTermTx});

            foreach (var otherMiner in miners.Where(m => m.PublicKey != extraBlockProducer.PublicKey))
            {
                await otherMiner.ExecuteBlock(block, new List<Transaction> {nextTermTx});
            }

            return miners.First(m => m.PublicKey == extraBlockProducer.PublicKey);
        }

        public static async Task<Round> GetCurrentRoundInformationAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var result = await contractTester.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract
                .GetCurrentRoundInformation), new Empty());
            return Round.Parser.ParseFrom(result.ReturnValue);
        }

        public static async Task<long> GetCurrentTermNumber(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var result = await contractTester.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract
                .GetCurrentTermNumber), new Empty());
            return SInt64Value.Parser.ParseFrom(result.ReturnValue).Value;
        }

        public static async Task<TransactionResult> TransferTokenAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester,
            Address receiverAddress, long amount)
        {
            return await contractTester.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Transfer),
                new TransferInput
                {
                    To = receiverAddress,
                    Amount = amount,
                    Symbol = "ELF",
                });
        }

        public static async Task<List<TransactionResult>> TransferTokenAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester,
            List<Address> receiverAddresses, long amount)
        {
            var list = new List<TransactionResult>();
            foreach (var receiverAddress in receiverAddresses)
            {
                var result = await contractTester.ExecuteTokenContractMethodWithMiningAsync(
                    nameof(TokenContract.Transfer),
                    new TransferInput
                    {
                        To = receiverAddress,
                        Amount = amount,
                        Symbol = "ELF",
                    });
                list.Add(result);
            }

            return list;
        }

        public static async Task<long> GetBalanceAsync(this ContractTester<DPoSContractTestAElfModule> contractTester,
            Address targetAddress)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = targetAddress,
                    Symbol = "ELF"
                });
            var balanceOutput = GetBalanceOutput.Parser.ParseFrom(bytes);
            return balanceOutput.Balance;
        }

        #endregion

        #region Election

        public static async Task<TransactionResult> AnnounceElectionAsync(
            this ContractTester<DPoSContractTestAElfModule> candidate, string alias = null)
        {
            if (alias == null)
            {
                alias = candidate.KeyPair.PublicKey.ToHex().Substring(0, DPoSContractConsts.AliasLimit);
            }

            return await candidate.ExecuteContractWithMiningAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.AnnounceElection),
                new Alias() {Value = alias});
        }

        public static async Task<TransactionResult> QuitElectionAsync(
            this ContractTester<DPoSContractTestAElfModule> candidate)
        {
            return await candidate.ExecuteContractWithMiningAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.QuitElection), new Empty());
        }

        public static async Task<List<ContractTester<DPoSContractTestAElfModule>>> GenerateCandidatesAsync(
            this ContractTester<DPoSContractTestAElfModule> starter, int number)
        {
            var candidatesKeyPairs = new List<ECKeyPair>();
            var candidates = new List<ContractTester<DPoSContractTestAElfModule>>();
            var transferTxs = new List<Transaction>();
            var announceElectionTxs = new List<Transaction>();

            for (var i = 0; i < number; i++)
            {
                var candidateKeyPair = CryptoHelpers.GenerateKeyPair();
                transferTxs.Add(await starter.GenerateTransactionAsync(starter.GetTokenContractAddress(),
                    nameof(TokenContract.Transfer), starter.KeyPair, new TransferInput
                    {
                        To = Address.FromPublicKey(candidateKeyPair.PublicKey),
                        Amount = DPoSContractConsts.LockTokenForElection,
                        Symbol = "ELF"
                    }));
                announceElectionTxs.Add(await starter.GenerateTransactionAsync(starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.AnnounceElection), candidateKeyPair,
                    new Alias
                    {
                        Value = $"{candidateKeyPair.PublicKey.ToHex().Substring(0, DPoSContractConsts.AliasLimit)}"
                    }));
                candidatesKeyPairs.Add(candidateKeyPair);
            }

            // Package Transfer txs.
            await starter.MineAsync(transferTxs);

            // Package AnnounceElection txs.
            await starter.MineAsync(announceElectionTxs);

            foreach (var candidatesKeyPair in candidatesKeyPairs)
            {
                candidates.Add(starter.CreateNewContractTester(candidatesKeyPair));
            }

            return candidates;
        }

        /// <summary>
        /// Default pocket money is 10000L.
        /// </summary>
        /// <param name="starter"></param>
        /// <param name="number"></param>
        /// <param name="pocketMoney"></param>
        /// <returns></returns>
        public static async Task<List<ContractTester<DPoSContractTestAElfModule>>> GenerateVotersAsync(
            this ContractTester<DPoSContractTestAElfModule> starter, int number = 1, long pocketMoney = 10000L)
        {
            var voters = new List<ContractTester<DPoSContractTestAElfModule>>();

            for (var i = 0; i < number; i++)
            {
                var voter = starter.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
                await starter.TransferTokenAsync(voter.GetCallOwnerAddress(), pocketMoney);
                voters.Add(voter);
            }

            return voters;
        }

        public static async Task<TransactionResult> Vote(this ContractTester<DPoSContractTestAElfModule> voter,
            string publicKey,
            long amount, int lockTime)
        {
            return await voter.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.Vote),
                new VoteInput()
                {
                    CandidatePublicKey = publicKey,
                    Amount = amount,
                    LockTime = lockTime
                });
        }

        public static async Task<StringList> GetCandidatesListAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidatesList), new Empty());
            return StringList.Parser.ParseFrom(bytes);
        }

        public static async Task<Candidates> GetCandidatesAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidates), new Empty());
            return Candidates.Parser.ParseFrom(bytes);
        }

        public static async Task<Tickets> GetTicketsInformationAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsInformation), new PublicKey()
                {
                    Hex = contractTester.PublicKey
                });
            return Tickets.Parser.ParseFrom(bytes);
        }

        public static async Task<VotingRecord> GetVotingRecord(
            this ContractTester<DPoSContractTestAElfModule> contractTester, Hash txId)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetVotingRecord), txId);
            return VotingRecord.Parser.ParseFrom(bytes);
        }

        public static async Task<Tickets> GetPageableTicketsInfo(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string publicKey, int startIndex,
            int length)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableTicketsInfo),
                new PageableTicketsInfoInput()
                {
                    PublicKey = publicKey,
                    Start = startIndex,
                    Length = length
                });
            return Tickets.Parser.ParseFrom(bytes);
        }

        public static async Task<Tickets> GetPageableNotWithdrawnTicketsInfo(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string publicKey, int startIndex,
            int length)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableNotWithdrawnTicketsInfo),
                new PageableTicketsInfoInput()
                {
                    PublicKey = publicKey,
                    Start = startIndex,
                    Length = length
                });
            return Tickets.Parser.ParseFrom(bytes);
        }

        #endregion

        #region Dividends

        public static async Task<LongList> CheckDividendsOfPreviousTerm(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(
                contractTester.GetDividendsContractAddress(),
                nameof(DividendContract.CheckDividendsOfPreviousTerm),
                new Empty());
            return LongList.Parser.ParseFrom(bytes);
        }

        #endregion

        public static async Task SetBlockchainAgeAsync(this ContractTester<DPoSContractTestAElfModule> starter,
            long age)
        {
            await starter.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.SetBlockchainAge),
                new SInt64Value()
                {
                    Value = age
                });
        }

        public static ContractTester<DPoSContractTestAElfModule> AnyOne(
            this List<ContractTester<DPoSContractTestAElfModule>> contractTesters)
        {
            return contractTesters[new Random().Next(0, contractTesters.Count)];
        }

        #region LIB

        public static async Task<long> GetLIBOffset(this ContractTester<DPoSContractTestAElfModule> miner)
        {
            return SInt64Value.Parser.ParseFrom((await miner.CallContractMethodAsync(
                miner.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetLIBOffset),
                new Empty()
            ))).Value;
        }

        #endregion
    }
}