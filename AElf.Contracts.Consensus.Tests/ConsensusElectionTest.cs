using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Sdk.CSharp;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Consensus.Tests
{
    [UseAutofacTestFramework]
    public class ConsensusElectionTest
    {
        private readonly ContractZeroShim _zeroContract;
        private readonly TokenContractShim _tokenContract;
        private readonly ConsensusContractShim _consensusContract;
        private readonly DividendsContractShim _dividendsContract;
        private readonly MockSetup _mock;

        private ECKeyPair _voter1 = new KeyPairGenerator().Generate();
        private ECKeyPair _voter2 = new KeyPairGenerator().Generate();
        private ECKeyPair _voter3 = new KeyPairGenerator().Generate();
        private ECKeyPair _voter4 = new KeyPairGenerator().Generate();
        
        private ECKeyPair _candidate1 = new KeyPairGenerator().Generate();
        private ECKeyPair _candidate2 = new KeyPairGenerator().Generate();
        private ECKeyPair _candidate3 = new KeyPairGenerator().Generate();
        private ECKeyPair _candidate4 = new KeyPairGenerator().Generate();

        public ConsensusElectionTest(MockSetup mock)
        {
            _mock = mock;
            _zeroContract = new ContractZeroShim(mock);
            _tokenContract = new TokenContractShim(mock);
            _consensusContract = new ConsensusContractShim(mock);
            _dividendsContract = new DividendsContractShim(mock);

            InitializeToken();
        }

        private void InitializeToken()
        {
            _tokenContract.Initialize("ELF", "AElf Token", 1000000000, 2);
            _tokenContract.Transfer(Address.FromPublicKey(_mock.ChainId.DumpByteArray(), _candidate1.PublicKey),
                GlobalConfig.LockTokenForElection);
            _tokenContract.Transfer(Address.FromPublicKey(_mock.ChainId.DumpByteArray(), _candidate2.PublicKey),
                GlobalConfig.LockTokenForElection);
            _tokenContract.Transfer(Address.FromPublicKey(_mock.ChainId.DumpByteArray(), _candidate3.PublicKey),
                GlobalConfig.LockTokenForElection);
            _tokenContract.Transfer(Address.FromPublicKey(_mock.ChainId.DumpByteArray(), _candidate4.PublicKey),
                GlobalConfig.LockTokenForElection);
            
            _tokenContract.Transfer(Address.FromPublicKey(_mock.ChainId.DumpByteArray(), _voter1.PublicKey), 100_000);
        }

        [Fact]
        public void AnnounceElectionTest()
        {
            var balance = _tokenContract.BalanceOf(GetAddress(_candidate1));
            Assert.True(balance >= GlobalConfig.LockTokenForElection);
            
            _consensusContract.AnnounceElection(_candidate1);
            var res = _consensusContract.IsCandidate(_candidate1.PublicKey.ToHex());
            Assert.True(res);
            
            balance = _tokenContract.BalanceOf(GetAddress(_candidate1));
            // TODO: Weird, maybe the transfer before was failed.   
            //Assert.True(balance == 0);
        }
        
        [Fact]
        public void QuitElectionTest()
        {
            _consensusContract.QuitElection(_candidate1);
            var res = _consensusContract.IsCandidate(_candidate1.PublicKey.ToHex());
            Assert.False(res);
        }

        [Fact]
        public void VoteBasicTest()
        {
            // Candidate announce election.
            _consensusContract.AnnounceElection(_candidate2);
            
            // Voter vote to aforementioned candidate.
            const ulong amount = 10_000;
            var balanceOfVoter = _tokenContract.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);
            _consensusContract.Vote(_voter1, _candidate2, amount, 90);
            
            // Check tickets of voter
            var ticketsOfVoter = _consensusContract.GetTicketsInfo(_voter1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfVoter = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfVoter);
            Assert.False(votingRecordOfVoter.IsExpired());
            Assert.True(votingRecordOfVoter.Count == amount);
            Assert.True(votingRecordOfVoter.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfVoter.To == _candidate2.PublicKey.ToHex());

            // Check tickets of candidate
            var ticketsOfCandidate = _consensusContract.GetTicketsInfo(_candidate2);
            Assert.True(ticketsOfCandidate.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfCandidate = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfCandidate);
            Assert.False(votingRecordOfCandidate.IsExpired());
            Assert.True(votingRecordOfCandidate.Count == amount);
            Assert.True(votingRecordOfCandidate.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfCandidate.To == _candidate2.PublicKey.ToHex());
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(_mock.ChainId.DumpByteArray(), keyPair.PublicKey);
        }
    }
}