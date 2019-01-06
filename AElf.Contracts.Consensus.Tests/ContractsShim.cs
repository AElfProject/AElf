using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Types.CSharp;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.Tests
{
    public class ContractsShim
    {
        private readonly MockSetup _mock;

        private readonly SimpleExecutingService _executingService;

        public TransactionContext TransactionContext { get; private set; }

        private static Address Sender => Address.Zero;

        private static ECKeyPair SenderKeyPair => new KeyPairGenerator().Generate();

        public Address ConsensusContractAddress { get; }
        private Address TokenContractAddress { get; }
        public Address DividendsContractAddress { get; }

        public ContractsShim(MockSetup mock, SimpleExecutingService executingService)
        {
            _mock = mock;
            _executingService = executingService;

            DeployConsensusContractAsync();
            DeployTokenContractAsync();
            DeployDividendsContractAsync();

            ConsensusContractAddress = ContractHelpers.GetConsensusContractAddress(_mock.ChainId);
            TokenContractAddress = ContractHelpers.GetTokenContractAddress(_mock.ChainId);
            DividendsContractAddress = ContractHelpers.GetDividendsContractAddress(_mock.ChainId);
        }

        #region Consensus.Query

        public Round GetRoundInfo(ulong roundNumber)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetRoundInfo), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Round>();
        }

        public ulong GetCurrentRoundNumber()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentRoundNumber), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetCurrentTermNumber()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentTermNumber), SenderKeyPair);
            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }

        public bool? IsCandidate(string publicKey)
        {
            ExecuteAction(ConsensusContractAddress, nameof(IsCandidate), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
        }

        public StringList GetCandidatesList()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCandidatesList), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public string GetCandidatesListToFriendlyString()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCandidatesListToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public CandidateInHistory GetCandidateHistoryInfo()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCandidateHistoryInfo), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<CandidateInHistory>();
        }

        public string GetCandidateHistoryInfoToFriendlyString()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCandidateHistoryInfoToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Miners GetCurrentMiners()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentMiners), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Miners>();
        }

        public string GetCurrentMinersToFriendlyString()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentMinersToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Tickets GetTicketsInfo(string publicKey)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTicketsInfo), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public string GetTicketsInfoToFriendlyString(string publicKey)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTicketsInfoToFriendlyString), SenderKeyPair,
                publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public TicketsDictionary GetCurrentElectionInfo(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTicketsInfo), SenderKeyPair, startIndex, length, orderBy);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TicketsDictionary>();
        }

        public string GetCurrentElectionInfoToFriendlyString(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTicketsInfo), SenderKeyPair, startIndex, length, orderBy);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong GetBlockchainAge()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentVictories), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public StringList GetCurrentVictories()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentVictories), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public string GetCurrentVictoriesToFriendlyString()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentVictoriesToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTermSnapshot), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TermSnapshot>();
        }

        public string GetTermSnapshotToFriendlyString(ulong termNumber)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTermSnapshotToFriendlyString), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public string QueryAlias(string publicKey)
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryAlias), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTermNumberByRoundNumber), SenderKeyPair, roundNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetVotesCount()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetVotesCount), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetTicketsCount()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetTicketsCount), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong QueryCurrentDividendsForVoters()
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryCurrentDividendsForVoters), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong QueryCurrentDividends()
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryCurrentDividends), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public StringList QueryAliasesInUse()
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryAliasesInUse), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryMinedBlockCountInCurrentTerm), SenderKeyPair,
                publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public string QueryAliasesInUseToFriendlyString()
        {
            ExecuteAction(ConsensusContractAddress, nameof(QueryAliasesInUseToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        #endregion

        #region Consensus.Process

        public void InitialTerm(ECKeyPair minerKeyPair, Term initialTerm)
        {
            ExecuteAction(ConsensusContractAddress, nameof(InitialTerm), minerKeyPair, initialTerm, 1);
        }

        public void NextTerm(ECKeyPair minerKeyPair, Term nextTerm)
        {
            ExecuteAction(ConsensusContractAddress, nameof(NextTerm), minerKeyPair, nextTerm);
        }

        public void PackageOutValue(ECKeyPair minerKeyPair, ToPackage toPackage)
        {
            ExecuteAction(ConsensusContractAddress, nameof(PackageOutValue), minerKeyPair, toPackage);
        }

        public void BroadcastInValue(ECKeyPair minerKeyPair, ToBroadcast toBroadcast)
        {
            ExecuteAction(ConsensusContractAddress, nameof(BroadcastInValue), minerKeyPair, toBroadcast);
        }

        public void NextRound(ECKeyPair minerKeyPair, Forwarding forwarding)
        {
            ExecuteAction(ConsensusContractAddress, nameof(NextRound), minerKeyPair, forwarding);
        }

        #endregion

        #region Consensus.Election

        public void AnnounceElection(ECKeyPair candidateKeyPair, string alias = "")
        {
            ExecuteAction(ConsensusContractAddress, nameof(AnnounceElection), candidateKeyPair, alias);
        }

        public void QuitElection(ECKeyPair candidateKeyPair)
        {
            ExecuteAction(ConsensusContractAddress, nameof(QuitElection), candidateKeyPair);
        }

        public void Vote(ECKeyPair voterKeyPair, string candidatePublicKey, ulong amount, int lockDays)
        {
            ExecuteAction(ConsensusContractAddress, nameof(Vote), voterKeyPair, candidatePublicKey, amount, lockDays);
        }

        public void ReceiveAllDividends(ECKeyPair ownerKeyPair)
        {
            ExecuteAction(ConsensusContractAddress, nameof(ReceiveAllDividends), ownerKeyPair);
        }

        public void WithdrawAll(ECKeyPair ownerKeyPair)
        {
            ExecuteAction(ConsensusContractAddress, nameof(WithdrawAll), ownerKeyPair);
        }

        public void InitialBalance(ECKeyPair minerKeyPair, Address address, ulong amount)
        {
            ExecuteAction(ConsensusContractAddress, nameof(InitialBalance), minerKeyPair, address, amount);
        }

        #endregion Consensus.Election

        #region Dividends

        public ulong GetTermDividends(ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetTermDividends), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetTermTotalWeights(ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetTermTotalWeights), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetLatestRequestDividendsTermNumber(VotingRecord votingRecord)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetLatestRequestDividendsTermNumber), SenderKeyPair,
                votingRecord);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAvailableDividends(VotingRecord votingRecord)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetAvailableDividends), SenderKeyPair, votingRecord);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAllAvailableDividends(string publicKey)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetAllAvailableDividends), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAvailableDividendsByVotingInformation(Hash transactionId, ulong termNumber, ulong weight)
        {
            ExecuteAction(DividendsContractAddress, nameof(GetAvailableDividendsByVotingInformation), SenderKeyPair,
                transactionId,
                termNumber, weight);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckDividends(ulong ticketsAmount, int lockTime, ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(CheckDividends), SenderKeyPair, ticketsAmount,
                lockTime, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckDividendsOfPreviousTerm(ulong ticketsAmount, int lockTime)
        {
            ExecuteAction(DividendsContractAddress, nameof(CheckDividendsOfPreviousTerm), SenderKeyPair, ticketsAmount,
                lockTime);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckStandardDividends(ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(CheckStandardDividends), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckStandardDividendsOfPreviousTerm()
        {
            ExecuteAction(DividendsContractAddress, nameof(GetTermDividends), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public void TransferDividends(VotingRecord votingRecord, ulong maxTermNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(TransferDividends), SenderKeyPair, votingRecord,
                maxTermNumber);
        }

        public void AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            ExecuteAction(DividendsContractAddress, nameof(AddDividends), SenderKeyPair, termNumber, dividendsAmount);
        }

        public void AddWeights(ulong weights, ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(AddWeights), SenderKeyPair, weights, termNumber);
        }

        public void KeepWeights()
        {
            ExecuteAction(DividendsContractAddress, nameof(KeepWeights), SenderKeyPair);
        }

        public void SubWeights(ulong weights, ulong termNumber)
        {
            ExecuteAction(DividendsContractAddress, nameof(SubWeights), SenderKeyPair, weights, termNumber);
        }

        #endregion

        #region Token

        public ulong BalanceOf(Address owner)
        {
            ExecuteAction(TokenContractAddress, nameof(BalanceOf), SenderKeyPair, owner);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public void Transfer(ECKeyPair callerKeyPair, Address to, ulong amount)
        {
            ExecuteAction(TokenContractAddress, nameof(BalanceOf), callerKeyPair, to, amount);
        }

        #endregion

        #region Private methods

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_mock.StateManager);
        }

        private void DeployConsensusContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1,
                    MockSetup.GetContractCode(_mock.ConsensusContractName)))
            });
        }

        private void DeployTokenContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, MockSetup.GetContractCode(_mock.TokenContractName)))
            });
        }

        private void DeployDividendsContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1,
                    MockSetup.GetContractCode(_mock.DividendsContractName)))
            });
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        private void ExecuteTransaction(Transaction tx)
        {
            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                Hash.FromString(GlobalConfig.DefaultChainId), DateTime.UtcNow, new CancellationToken(), null,
                TransactionType.ContractTransaction, true).Result;
            foreach (var transactionTrace in traces)
            {
                CommitChangesAsync(transactionTrace).Wait();

                TransactionContext = new TransactionContext
                {
                    Trace = transactionTrace
                };
            }
        }

        private void ExecuteAction(Address contractAddress, string methodName, ECKeyPair callerKeyPair,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signer = new ECSigner();
            var signature = signer.Sign(callerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        #endregion
    }
}