using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ElectionTest
    {
        [Fact]
        public async Task AnnounceElectionTest()
        {
            // The starter initial chain and tokens.
            var starter = new ContractTester<DPoSContractTestAElfModule>();
            await starter.InitialChainAndTokenAsync();
            var starterBalance = await starter.GetBalanceAsync(starter.GetCallOwnerAddress());
            Assert.Equal(DPoSContractConsts.LockTokenForElection * 100, starterBalance);
            
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateKeyPair = CryptoHelpers.GenerateKeyPair();
            var candidateAddress = Address.FromPublicKey(candidateKeyPair.PublicKey);
            await starter.TransferTokenAsync(candidateAddress, DPoSContractConsts.LockTokenForElection + 100);
            var candidate = starter.CreateNewContractTester(candidateKeyPair);
            var balance = await starter.GetBalanceAsync(candidateAddress);
            Assert.Equal(DPoSContractConsts.LockTokenForElection + 100, balance);
            
            // The candidate announce election.
            await candidate.AnnounceElectionAsync("AElfin");
            var candidatesList = await candidate.GetCandidatesListAsync();

            // Check the candidates list.
            Assert.Contains(candidate.KeyPair.PublicKey.ToHex(), candidatesList.Values.ToList());
        }

        [Fact]
        public async Task VoteTest()
        {
            const ulong amount = 1000;
            
            var starter = new ContractTester<DPoSContractTestAElfModule>();
            await starter.InitialChainAndTokenAsync();

            var candidate = (await starter.GenerateCandidatesAsync(1))[0];

            var voter = starter.GenerateVoters(1)[0];
            await starter.TransferTokenAsync(voter.GetCallOwnerAddress(), 10000);

            await voter.Vote(candidate.PublicKey, amount, 100);

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();

            Assert.Equal(amount, ticketsOfCandidate.ObtainedTickets);
            
            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            
            Assert.Equal(amount, ticketsOfVoter.VotedTickets);
        }
    }
}