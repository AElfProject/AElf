using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Dividends;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using System;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public static class ContractTesterExtensions
    {
        #region IConsensusSmartContract

        public static async Task<ConsensusCommand> GetConsensusCommandAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            Timestamp timestamp = null)
        {
            var triggerInformation = new DPoSTriggerInformation
            {
                Timestamp = timestamp ?? DateTime.UtcNow.ToTimestamp(),
                PublicKey = tester.KeyPair.PublicKey.ToHex(),
                IsBootMiner = true,
            };
            var bytes = await tester.CallContractMethodAsync(
                tester.GetConsensusContractAddress(), // Usually the second contract is consensus contract.
                ConsensusConsts.GetConsensusCommand,
                triggerInformation.ToByteArray());
            return ConsensusCommand.Parser.ParseFrom(bytes);
        }

        public static async Task<DPoSInformation> GetNewConsensusInformationAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GetNewConsensusInformation, triggerInformation.ToByteArray());
            return DPoSInformation.Parser.ParseFrom(bytes);
        }

        public static async Task<List<Transaction>> GenerateConsensusTransactionsAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions, triggerInformation.ToByteArray());
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            tester.SupplyTransactionParameters(ref txs);

            return txs;
        }

        public static async Task<ValidationResult> ValidateConsensusAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSInformation information)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.ValidateConsensus, information.ToByteArray());
            return ValidationResult.Parser.ParseFrom(bytes);
        }

        public static async Task<Block> GenerateConsensusTransactionsAndMineABlockAsync(
            this ContractTester<DPoSContractTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation,
            params ContractTester<DPoSContractTestAElfModule>[] testersGonnaExecuteThisBlock)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions,
                triggerInformation.ToByteArray());
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

        public static async Task ExecuteConsensusContractMethodWithMiningAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, params object[] objects)
        {
            await contractTester.ExecuteContractWithMiningAsync(contractTester.GetConsensusContractAddress(),
                methodName, objects);
        }

        public static async Task InitializeAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            await contractTester.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.Initialize),
                contractTester.GetContractAddress(typeof(TokenContract)),
                contractTester.GetContractAddress(typeof(DividendsContract)));
        }

        #endregion

        #region Handle Token

        public static Address GetTokenContractAddress(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(typeof(TokenContract));
        }

        public static async Task ExecuteTokenContractMethodWithMiningAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, params object[] objects)
        {
            await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                methodName, objects);
        }
        
        public static async Task InitialChainAndTokenAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            await contractTester.InitialChainAsync(typeof(TokenContract), typeof(DividendsContract));

            // Initial token.
            await contractTester.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Initialize),
                "ELF", "elf token", DPoSContractConsts.LockTokenForElection * 100, 2U);

            // Initial consensus contract.
            await contractTester.InitializeAsync();
            
            // Set consensus contract address to token contract.
            await contractTester.ExecuteTokenContractMethodWithMiningAsync(
                nameof(TokenContract.SetConsensusContractAddress), contractTester.GetConsensusContractAddress());
        }
        
        public static async Task TransferTokenAsync(this ContractTester<DPoSContractTestAElfModule> contractTester,
            Address receiverAddress, ulong amount)
        {
            await contractTester.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Transfer),
                receiverAddress, amount);
        }
        
        public static async Task TransferTokenAsync(this ContractTester<DPoSContractTestAElfModule> contractTester,
            List<Address> receiverAddresses, ulong amount)
        {
            foreach (var receiverAddress in receiverAddresses)
            {
                await contractTester.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Transfer),
                    receiverAddress, amount);
            }
        }

        public static async Task<ulong> GetBalanceAsync(this ContractTester<DPoSContractTestAElfModule> contractTester,
            Address targetAddress)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.BalanceOf), targetAddress);
            return bytes.DeserializeToUInt64();
        }

        #endregion

        #region Election

        public static async Task AnnounceElectionAsync(this ContractTester<DPoSContractTestAElfModule> candidiate, string alias = null)
        {
            if (alias == null)
            {
                alias = candidiate.KeyPair.PublicKey.ToHex().Substring(0, DPoSContractConsts.AliasLimit);
            }

            await candidiate.ExecuteContractWithMiningAsync(candidiate.GetConsensusContractAddress(),
                nameof(ConsensusContract.AnnounceElection), alias);
        }

        public static async Task<List<ContractTester<DPoSContractTestAElfModule>>> GenerateCandidatesAsync(
            this ContractTester<DPoSContractTestAElfModule> starter, int number)
        {
            var candidates = new List<ContractTester<DPoSContractTestAElfModule>>();

            for (var i = 0; i < number; i++)
            {
                var candidateKeyPair = CryptoHelpers.GenerateKeyPair();
                var candidate = starter.CreateNewContractTester(candidateKeyPair);
                await starter.TransferTokenAsync(candidate.GetCallOwnerAddress(),
                    DPoSContractConsts.LockTokenForElection + 100);
                await candidate.AnnounceElectionAsync();
                candidates.Add(candidate);
            }

            return candidates;
        }
        
        public static List<ContractTester<DPoSContractTestAElfModule>> GenerateVoters(
            this ContractTester<DPoSContractTestAElfModule> starter, int number)
        {
            var voters = new List<ContractTester<DPoSContractTestAElfModule>>();

            for (var i = 0; i < number; i++)
            {
                voters.Add(starter.CreateNewContractTester(CryptoHelpers.GenerateKeyPair()));
            }

            return voters;
        }

        public static async Task Vote(this ContractTester<DPoSContractTestAElfModule> voter, string publicKey,
            ulong amount, int lockTime)
        {
            await voter.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.Vote), publicKey,
                amount, lockTime);
        }

        public static async Task<StringList> GetCandidatesListAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidatesList));
            return StringList.Parser.ParseFrom(bytes);
        }

        public static async Task<Tickets> GetTicketsInformationAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsInformation), contractTester.PublicKey);
            return Tickets.Parser.ParseFrom(bytes);
        }

        #endregion
    }
}