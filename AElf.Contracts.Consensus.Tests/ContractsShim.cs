using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types.CSharp;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.Tests
{
    // ReSharper disable UnusedMember.Global
    public class ContractsShim : ITransientDependency
    {
        private readonly MockSetup _mock;

        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IBlockchainService _blockchainService;

        public TransactionContext TransactionContext { get; private set; }

        private static Address Sender => Address.Generate();

        private static ECKeyPair SenderKeyPair => CryptoHelpers.GenerateKeyPair();

        public Address ConsensusContractAddress { get; }
        private Address TokenContractAddress { get; }
        public Address DividendsContractAddress { get; }

        public ContractsShim(MockSetup mock, ITransactionExecutingService transactionExecutingService,
            IBlockchainService blockchainService)
        {
            _mock = mock;
            _transactionExecutingService = transactionExecutingService;
            _blockchainService = blockchainService;

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
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetRoundInfo), SenderKeyPair, roundNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Round>();
        }

        public ulong GetCurrentRoundNumber()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentRoundNumber), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetCurrentTermNumber()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentTermNumber), SenderKeyPair);
            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }

        public bool? IsCandidate(string publicKey)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(IsCandidate), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
        }

        public StringList GetCandidatesList()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCandidatesList), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public string GetCandidatesListToFriendlyString()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCandidatesListToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public CandidateInHistory GetCandidateHistoryInfo()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCandidateHistoryInfo), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<CandidateInHistory>();
        }

        public string GetCandidateHistoryInfoToFriendlyString()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCandidateHistoryInfoToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Miners GetCurrentMiners()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentMiners), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Miners>();
        }

        public string GetCurrentMinersToFriendlyString()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentMinersToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Tickets GetTicketsInfo(string publicKey)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTicketsInfo), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public string GetTicketsInfoToFriendlyString(string publicKey)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTicketsInfoToFriendlyString), SenderKeyPair,
                publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Tickets GetPageableTicketsInfo(string publicKey, int startIndex, int length)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetPageableTicketsInfo), SenderKeyPair,
                publicKey, startIndex, length);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public TicketsDictionary GetPageableElectionInfo(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetPageableElectionInfo), SenderKeyPair, startIndex, length, orderBy);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TicketsDictionary>();
        }

        public string GetPageableElectionInfoToFriendlyString(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetPageableElectionInfoToFriendlyString), SenderKeyPair, startIndex, length, orderBy);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong GetBlockchainAge()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetBlockchainAge), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public StringList GetCurrentVictories()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentVictories), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public string GetCurrentVictoriesToFriendlyString()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCurrentVictoriesToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTermSnapshot), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TermSnapshot>();
        }

        public string GetTermSnapshotToFriendlyString(ulong termNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTermSnapshotToFriendlyString), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public string QueryAlias(string publicKey)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryAlias), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTermNumberByRoundNumber), SenderKeyPair, roundNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetVotesCount()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetVotesCount), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetTicketsCount()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetTicketsCount), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong QueryCurrentDividendsForVoters()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryCurrentDividendsForVoters), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong QueryCurrentDividends()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryCurrentDividends), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public StringList QueryAliasesInUse()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryAliasesInUse), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryMinedBlockCountInCurrentTerm), SenderKeyPair,
                publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public string QueryAliasesInUseToFriendlyString()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QueryAliasesInUseToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }
        
        public CandidateInHistoryDictionary GetCandidatesHistoryInfo()
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(GetCandidatesHistoryInfo), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<CandidateInHistoryDictionary>();
        }

        #endregion

        #region Consensus.Process

        public void InitialTerm(ECKeyPair minerKeyPair, Term initialTerm)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(InitialTerm), minerKeyPair, initialTerm, 1);
        }

        public void NextTerm(ECKeyPair minerKeyPair, Term nextTerm)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(NextTerm), minerKeyPair, nextTerm);
        }

        public ActionResult SnapshotForTerm(ECKeyPair minerKeyPair, ulong targetTermNumber, ulong roundNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(SnapshotForTerm), minerKeyPair,
                targetTermNumber, roundNumber);

            return TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<ActionResult>();
        }
        
        public ActionResult SnapshotForMiners(ECKeyPair minerKeyPair, ulong targetTermNumber, ulong roundNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(SnapshotForMiners), minerKeyPair,
                targetTermNumber, roundNumber);

            return TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<ActionResult>();
        }
        
        public ActionResult SendDividends(ECKeyPair minerKeyPair, ulong targetTermNumber, ulong roundNumber)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(SendDividends), minerKeyPair,
                targetTermNumber, roundNumber);

            return TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<ActionResult>();
        }

        public void PackageOutValue(ECKeyPair minerKeyPair, ToPackage toPackage)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(PackageOutValue), minerKeyPair, toPackage);
        }

        public void BroadcastInValue(ECKeyPair minerKeyPair, ToBroadcast toBroadcast)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(BroadcastInValue), minerKeyPair, toBroadcast);
        }

        public void NextRound(ECKeyPair minerKeyPair, Forwarding forwarding)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(NextRound), minerKeyPair, forwarding);
        }

        #endregion

        #region Consensus.Election

        public void AnnounceElection(ECKeyPair candidateKeyPair, string alias = "")
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(AnnounceElection), candidateKeyPair, alias);
        }

        public void QuitElection(ECKeyPair candidateKeyPair)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(QuitElection), candidateKeyPair);
        }

        public void Vote(ECKeyPair voterKeyPair, string candidatePublicKey, ulong amount, int lockDays)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(Vote), voterKeyPair, candidatePublicKey, amount, lockDays);
        }

        public void ReceiveAllDividends(ECKeyPair ownerKeyPair)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(ReceiveAllDividends), ownerKeyPair);
        }

        public void WithdrawAll(ECKeyPair ownerKeyPair)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(WithdrawAll), ownerKeyPair, true);
        }
        
        public void WithdrawByTransactionId(ECKeyPair ownerKeyPair, string transactionId)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(WithdrawByTransactionId), ownerKeyPair, transactionId, true);
        }

        public void InitialBalance(ECKeyPair minerKeyPair, Address address, ulong amount)
        {
            _ = ExecuteAsync(ConsensusContractAddress, nameof(InitialBalance), minerKeyPair, address, amount);
        }

        #endregion Consensus.Election

        #region Dividends

        public ulong GetTermDividends(ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetTermDividends), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetTermTotalWeights(ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetTermTotalWeights), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetLatestRequestDividendsTermNumber(VotingRecord votingRecord)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetLatestRequestDividendsTermNumber), SenderKeyPair,
                votingRecord);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAvailableDividends(VotingRecord votingRecord)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetAvailableDividends), SenderKeyPair, votingRecord);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAllAvailableDividends(string publicKey)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetAllAvailableDividends), SenderKeyPair, publicKey);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong GetAvailableDividendsByVotingInformation(Hash transactionId, ulong termNumber, ulong weight)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(GetAvailableDividendsByVotingInformation), SenderKeyPair,
                transactionId,
                termNumber, weight);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckDividends(ulong ticketsAmount, int lockTime, ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(CheckDividends), SenderKeyPair, ticketsAmount,
                lockTime, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ULongList CheckDividendsOfPreviousTerm()
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(CheckDividendsOfPreviousTerm), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<ULongList>();
        }

        public string CheckDividendsOfPreviousTermToFriendlyString()
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(CheckDividendsOfPreviousTermToFriendlyString), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong CheckStandardDividends(ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(CheckStandardDividends), SenderKeyPair, termNumber);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckStandardDividendsOfPreviousTerm()
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(CheckStandardDividendsOfPreviousTerm), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        // ReSharper disable once InconsistentNaming
        public void TransferDividends(VotingRecord votingRecord)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(TransferDividends), SenderKeyPair, votingRecord);
        }

        public void AddDividends(ulong termNumber, ulong dividendsAmount)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(AddDividends), SenderKeyPair, termNumber, dividendsAmount);
        }

        public void AddWeights(ulong weights, ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(AddWeights), SenderKeyPair, weights, termNumber);
        }

        public void KeepWeights(ulong oldTermNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(KeepWeights), SenderKeyPair, oldTermNumber);
        }

        public void SubWeights(ulong weights, ulong termNumber)
        {
            _ = ExecuteAsync(DividendsContractAddress, nameof(SubWeights), SenderKeyPair, weights, termNumber);
        }

        #endregion

        #region Token

        public ulong BalanceOf(Address owner)
        {
            _ = ExecuteAsync(TokenContractAddress, nameof(BalanceOf), SenderKeyPair, owner);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public void Transfer(ECKeyPair callerKeyPair, Address to, ulong amount)
        {
            _ = ExecuteAsync(TokenContractAddress, nameof(BalanceOf), callerKeyPair, to, amount);
        }

        #endregion

        #region Private methods

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
            AsyncHelper.RunSync(() => _transactionExecutingService.ExecuteAsync(new ChainContext
                {
                    ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"),
                    BlockHash = Hash.Genesis,
                    BlockHeight = 1
                },
                new List<Transaction> {tx},
                DateTime.UtcNow, new CancellationToken()));
        }

        private async Task<ByteString> ExecuteAsync(Address contractAddress, string methodName, ECKeyPair callerKeyPair,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(new ChainContext
                {
                    ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"),
                    BlockHash = Hash.Genesis,
                    BlockHeight = 1
                }, 
                new List<Transaction> {tx},
                DateTime.UtcNow, new CancellationToken());
            return executionReturnSets.Last().ReturnValue;
        }

        #endregion
    }
}