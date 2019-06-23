using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using Hash = AElf.Types.Hash;
using Transaction = AElf.Types.Transaction;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppTransactionService : IApplicationService
    {
        /// <summary>
        /// excute raw transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

        /// <summary>
        /// Creates an unsigned serialized transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        /// <summary>
        /// Broadcast a transaction
        /// </summary>
        /// <returns></returns>
        Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

        /// <summary>
        /// Broadcast multiple transactions
        /// </summary>
        /// <returns></returns>
        Task<string[]> SendTransactionsAsync(SendTransactionsInput input);

        /// <summary>
        /// send a transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        Task<byte[]> GetContractFileDescriptorSetAsync(string address);

        Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
           string methodName);
    }

    /// <summary>
    /// transaction services
    /// </summary>
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppTransactionService" />
    public sealed class AppTransactionService : IAppTransactionService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITxHub _txHub;

        public ILocalEventBus LocalEventBus { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppTransactionService"/> class.
        /// </summary>
        /// <param name="transactionReadOnlyExecutionService">The transaction read only execution service.</param>
        /// <param name="blockchainService">The app block service.</param>
        /// <param name="txHub"></param>
        public AppTransactionService(
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService,
            ITxHub txHub)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _txHub = txHub;

            LocalEventBus = NullLocalEventBus.Instance;
        }

        #region execute transaction

        /// <summary>
        /// excute raw transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyReturnReadableValueAsync(transaction);
                return response;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyAsync(transaction);
                return response?.ToHex();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }

        #endregion execute transaction

        #region create transaction

        /// <summary>
        /// Creates an unsigned serialized transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
        {
            var transaction = new Transaction
            {
                From = Address.Parse(input.From),
                To = Address.Parse(input.To),
                RefBlockNumber = input.RefBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(Hash.LoadHex(input.RefBlockHash).Value.Take(4).ToArray()),
                MethodName = input.MethodName
            };
            var methodDescriptor = await GetContractMethodDescriptorAsync(Address.Parse(input.To), input.MethodName);
            if (methodDescriptor == null)
                throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                    Error.NoMatchMethodInContractAddress.ToString());
            try
            {
                var parameters = methodDescriptor.InputType.Parser.ParseJson(input.Params);
                if (!IsValidMessage(parameters))
                    throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
                transaction.Params = parameters.ToByteString();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
            }

            return new CreateRawTransactionOutput
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            };
        }

        #endregion create transaction



        #region get transaction pool

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            var queued = await _txHub.GetTransactionPoolSizeAsync();
            return new GetTransactionPoolStatusOutput
            {
                Queued = queued
            };
        }

        #endregion get transaction pool

        #region send transaction service

        /// <summary>
        /// Broadcast a transaction
        /// </summary>
        /// <returns></returns>
        public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
        {
            var txIds = await PublishTransactionsAsync(new[] { input.RawTransaction });
            return new SendTransactionOutput
            {
                TransactionId = txIds[0]
            };
        }

        /// <summary>
        /// Broadcast multiple transactions
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> SendTransactionsAsync(SendTransactionsInput input)
        {
            var txIds = await PublishTransactionsAsync(input.RawTransactions.Split(","));

            return txIds;
        }

        /// <summary>
        /// send a transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
        {
            var transaction = Transaction.Parser.ParseFrom(ByteArrayHelpers.FromHexString(input.Transaction));
            transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
            var txIds = await PublishTransactionsAsync(new[] { transaction.ToByteArray().ToHex() });

            var output = new SendRawTransactionOutput
            {
                TransactionId = txIds[0]
            };

            if (!input.ReturnTransaction) return output;

            var transactionDto = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            var contractMethodDescriptor =
                await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);

            var parameters = contractMethodDescriptor.InputType.Parser.ParseFrom(transaction.Params);

            transactionDto.Params = JsonFormatter.ToDiagnosticString(parameters);
            output.Transaction = transactionDto;

            return output;
        }

        /// <summary>
        /// Publishes the transactions asynchronous.
        /// </summary>
        /// <param name="rawTransactions">The raw transactions.</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException">
        /// </exception>
        private async Task<string[]> PublishTransactionsAsync(string[] rawTransactions)
        {
            var txIds = new string[rawTransactions.Length];
            var transactions = new List<Transaction>();
            for (var i = 0; i < rawTransactions.Length; i++)
            {
                Transaction transaction;
                try
                {
                    var hexString = ByteArrayHelpers.FromHexString(rawTransactions[i]);
                    transaction = Transaction.Parser.ParseFrom(hexString);
                }
                catch
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                if (!IsValidMessage(transaction))
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());

                var contractMethodDescriptor =
                    await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
                if (contractMethodDescriptor == null)
                    throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                        Error.NoMatchMethodInContractAddress.ToString());

                var parameters = contractMethodDescriptor.InputType.Parser.ParseFrom(transaction.Params);

                if (!IsValidMessage(parameters))
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
                }

                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                transactions.Add(transaction);
                txIds[i] = transaction.GetHash().ToHex();
            }

            await LocalEventBus.PublishAsync(new TransactionsReceivedEvent()
            {
                Transactions = transactions
            });
            return txIds;
        }

        #endregion send transaction service

        #region app contract service

        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
        {
            try
            {
                var result = await GetFileDescriptorSetAsync(Address.Parse(address));
                return result;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
        }

        /// <summary>
        /// Gets the contract method descriptor asynchronous.
        /// </summary>
        /// <param name="contractAddress">The contract address.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
            string methodName)
        {
            IEnumerable<FileDescriptor> fileDescriptors;
            try
            {
                fileDescriptors = await GetFileDescriptorsAsync(contractAddress);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidContractAddress],
                    Error.InvalidContractAddress.ToString());
            }

            foreach (var fileDescriptor in fileDescriptors)
            {
                var method = fileDescriptor.Services.Select(s => s.FindMethodByName(methodName)).FirstOrDefault();
                if (method == null) continue;
                return method;
            }

            return null;
        }

        /// <summary>
        /// Gets the file descriptors asynchronous.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        private async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, address);
        }

        private async Task<byte[]> GetFileDescriptorSetAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, address);
        }

        #endregion app contract service

        /// <summary>
        /// Calls the read only asynchronous.
        /// </summary>
        /// <param name="tx">The tx.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        ///
        private async Task<byte[]> CallReadOnlyAsync(Transaction tx)
        {
            var chainContext = await GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx,
                DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.Error))
                throw new Exception(trace.Error);

            return trace.ReturnValue.ToByteArray();
        }

        /// <summary>
        /// Calls the read only return readable value asynchronous.
        /// </summary>
        /// <param name="tx">The tx.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> CallReadOnlyReturnReadableValueAsync(Transaction tx)
        {
            var chainContext = await GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx,
                DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.Error))
                throw new Exception(trace.Error);

            return trace.ReadableReturnValue;
        }

        /// <summary>
        /// Gets the chain context asynchronous.
        /// </summary>
        /// <returns></returns>
        private async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }

        #region tool

        private bool IsValidMessage(IMessage message)
        {
            try
            {
                JsonFormatter.ToDiagnosticString(message);
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion tool
    }
}