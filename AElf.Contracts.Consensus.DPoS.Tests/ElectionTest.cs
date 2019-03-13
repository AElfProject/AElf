using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ElectionTest
    {
        public readonly ContractTester<DPoSContractTestAElfModule> Starter;
        public ElectionTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync());
         }
        
        [Fact]
        public async Task Announce_Election_Success()
        {
            var starterBalance = await Starter.GetBalanceAsync(Starter.GetCallOwnerAddress());
            Assert.Equal(DPoSContractConsts.LockTokenForElection * 100, starterBalance);
            
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateInformation = GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInformation, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInformation);
            Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);
            
            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInformation);
            await candidate.AnnounceElectionAsync("AElfin");
            var candidatesList = await candidate.GetCandidatesListAsync();

            // Check the candidates list.
            Assert.Contains(candidate.KeyPair.PublicKey.ToHex(), candidatesList.Values.ToList());
        }

        [Fact]
        public async Task Announce_Election_WithoutEnough_Token()
        {
            // The starter transfer not enough token 
            var candidateInformation = GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInformation, 50_000UL);
            var balance = await Starter.GetBalanceAsync(candidateInformation);
            balance.ShouldBe(50_000UL);
            
            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInformation);
            var result = await candidate.AnnounceElectionAsync("AElfin");
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient balance").ShouldBeTrue();
            var candidatesList = await candidate.GetCandidatesListAsync();
            candidatesList.Values.ToList().Contains(candidateInformation).ShouldBeFalse();
        }

        [Fact]
        public async Task Announce_Election_Twice()
        {
            // The starter transfer 200_000UL
            var candidateInfo = GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection*2);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection*2);
            
            var candidate = Starter.CreateNewContractTester(candidateInfo);
            
            // Announce election.
            {
                var result = await candidate.AnnounceElectionAsync("AElfin");
                result.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }

            // Announce election again.
            {
                var result = await candidate.AnnounceElectionAsync("AElfinAgain");
                result.Status.ShouldBe(TransactionResultStatus.Failed);
            }

            // Check balance.
            {
                balance = await Starter.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(DPoSContractConsts.LockTokenForElection);
            }
            
            // Check candidate list again.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Quit_Election_Success()
        {
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateInfo = GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);

            // Check balance.
            {
                var balance = await Starter.GetBalanceAsync(candidateInfo);
                Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);
            }

            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInfo);

            await candidate.AnnounceElectionAsync("AElfin");

            // Check balance.
            {
                var balance = await candidate.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(0UL);
            }

            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }

            // Quit election
            var result = await candidate.QuitElectionAsync();
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.Contains(candidateInfo).ShouldBeFalse();
            }

            // Check balance.
            {
                var balance = await candidate.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(DPoSContractConsts.LockTokenForElection);
            }
        }

        [Fact]
        public async Task Quit_Election_WithoutAnnounce()
        {
            var candidateInfo = GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection);
            
            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInfo);
            
            //select another account ,and quit election
            candidateInfo = GenerateNewUser();
            candidate = Starter.CreateNewContractTester(candidateInfo);
            var result = await candidate.QuitElectionAsync();
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Failed to remove this public key from candidates list.").ShouldBeTrue();
            
            balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(0UL);
        }
        
        [Fact]
        public async Task Vote_Candidate_Success()
        {
            const ulong amount = 1000;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = Starter.GenerateVoters(1)[0];
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), 10000);

            await voter.Vote(candidate.PublicKey, amount, 100);

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfCandidate.ObtainedTickets);
            
            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfVoter.VotedTickets);
        }

        [Fact]
        public async Task Vote_Not_Candidate()
        {
            const ulong amount = 1000;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = Starter.GenerateVoters(1)[0];
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), amount);

            var notCandidate = GenerateNewUser();
            var result = await voter.Vote(notCandidate, amount, 100);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Target didn't announce election.").ShouldBeTrue();
            
            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(amount);
        }

        [Fact]
        public async Task Vote_Candidate_Without_Enough_Token()
        {
            const ulong amount = 100;
            const ulong voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = Starter.GenerateVoters(1)[0];
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), amount);

            var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Insufficient balance.").ShouldBeTrue();
            
            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(0UL);
        }

        [Fact]
        public async Task Vote_Same_Candidate_MultipleTimes()
        {
            const ulong amount = 1000;
            const ulong voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = Starter.GenerateVoters(1)[0];
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), amount);

            for (int i = 0; i < 5; i++)
            {
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(1000UL);

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(0UL);
        }

        [Fact]
        public async Task Vote_Different_Candidates()
        {
            const ulong amount = 1000;
            const ulong voteAmount = 200;
            var candidateLists = await Starter.GenerateCandidatesAsync(5);
            
            var voter = Starter.GenerateVoters(1)[0];
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), amount);

            for (int i = 0; i < 5; i++)
            {
                var candidate = candidateLists[i];
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(1000UL);

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(0UL);
        }

        [Fact]
        public async Task IsCandidate_Success()
        {
            var candidateLists = await Starter.GenerateCandidatesAsync(2);
            var nonCandidateInfo = GenerateNewUser();
            var candidate = Starter.CreateNewContractTester(nonCandidateInfo.KeyPair);
            var candidateResult = await candidate.CallContractMethodAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.IsCandidate), nonCandidateInfo.PublicKey);
            candidateResult.DeserializeToBool().ShouldBeFalse();
            
            var candidateResult1 = await candidate.CallContractMethodAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.IsCandidate), candidateLists[0].PublicKey);
            candidateResult1.DeserializeToBool().ShouldBeTrue();
        }

        private static User GenerateNewUser()
        {
            var callKeyPair = CryptoHelpers.GenerateKeyPair();
            var callAddress = Address.FromPublicKey(callKeyPair.PublicKey);
            var callPublicKey = callKeyPair.PublicKey.ToHex();

            return new User
            {
                KeyPair = callKeyPair,
                Address = callAddress,
                PublicKey = callPublicKey
            };
        }
        
        private struct User
        {
            public ECKeyPair KeyPair { get; set; }
            public Address Address { get; set; }
            public string PublicKey { get; set; }

            public static implicit operator ECKeyPair(User user)
            {
                return user.KeyPair;
            }
            
            public static implicit operator Address(User user)
            {
                return user.Address;
            }
            
            public static implicit operator string(User user)
            {
                return user.PublicKey;
            }
        }
    }
}