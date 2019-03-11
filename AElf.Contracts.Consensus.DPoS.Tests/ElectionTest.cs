using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ElectionTest
    {

    }

    public static class ElectionContractTesterExtensions
    {
        public static Address GetTokenContractAddress(this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(typeof(TokenContract));
        }
        
        public static async Task ExecuteTokenContractMethodWithMining(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, params object[] objects)
        {
            await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(), methodName,
                objects);
        }

        public static async Task ExecuteConsensusContractMethodWithMining(
            this ContractTester<DPoSContractTestAElfModule> contractTester, string methodName, params object[] objects)
        {
            await contractTester.ExecuteContractWithMiningAsync(contractTester.GetConsensusContractAddress(), methodName,
                objects);
        }
        
        public static async Task InitialChainAndTokenAsync(
            this ContractTester<DPoSContractTestAElfModule> contractTester, params Type[] contractTypes)
        {
            await contractTester.InitialChainAsync(contractTypes);

            // Initial token.
            await contractTester.ExecuteTokenContractMethodWithMining(nameof(TokenContract.Initialize),
                "ELF", "elf token", 1000_000UL, 2U);
        }

        public static async Task Transfer(this ContractTester<DPoSContractTestAElfModule> contractTester,
            Address transferTo, ulong amount)
        {
            await contractTester.ExecuteTokenContractMethodWithMining(nameof(TokenContract.Transfer),
                transferTo, amount);
        }

        public static async Task AnnounceElection(this ContractTester<DPoSContractTestAElfModule> contractTester)
        {
            await contractTester.ExecuteContractWithMiningAsync(contractTester.GetConsensusContractAddress(),
                nameof(ConsensusContract.AnnounceElection));
        }

        public static async Task<List<ContractTester<DPoSContractTestAElfModule>>> GenerateCandidates(this ContractTester<DPoSContractTestAElfModule> contractTester, int number)
        {
            var candidates = new List<ContractTester<DPoSContractTestAElfModule>>();

            for (var i = 0; i < number; i++)
            {
                var candidateKeyPair = CryptoHelpers.GenerateKeyPair();
                var candidate = contractTester.CreateNewContractTester(candidateKeyPair);
                await candidate.AnnounceElection();
                candidates.Add(candidate);
            }

            return candidates;
        }
        
        public static List<ContractTester<DPoSContractTestAElfModule>> GenerateVoters(this ContractTester<DPoSContractTestAElfModule> contractTester, int number)
        {
            var voters = new List<ContractTester<DPoSContractTestAElfModule>>();

            for (var i = 0; i < number; i++)
            {
                voters.Add(contractTester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair()));
            }

            return voters;
        }
    }
}