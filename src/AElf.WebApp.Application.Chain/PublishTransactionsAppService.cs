using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace AElf.WebApp.Application.Chain
{
    public interface IPublishTransactionsAppService : IApplicationService
    {
        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);
        
        Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

        Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

        Task<string[]> SendTransactionsAsync(SendTransactionsInput input);
    }


    public class PublishTransactionsAppService : IPublishTransactionsAppService
    {
        public ILocalEventBus LocalEventBus { get; set; }


        private static IBlockchainService _blockchainService;
        private static ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;


        public PublishTransactionsAppService(IBlockchainService blockchainService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            LocalEventBus = NullLocalEventBus.Instance;


            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
        }


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


        /// <summary>
        /// send a transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
        {
            var transaction = Transaction.Parser.ParseFrom(ByteArrayHelpers.FromHexString(input.Transaction));
            transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
            var txIds = await PublishTransactionsAsync(new[] {transaction.ToByteArray().ToHex()});

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
        /// Broadcast a transaction
        /// </summary>
        /// <returns></returns>
        public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
        {
            var txIds = await PublishTransactionsAsync(new[] {input.RawTransaction});
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
        
        private async Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
            string methodName)
        {
            return await ContractMethodDescriptorHelper.GetContractMethodDescriptorAsync(_blockchainService,
                _transactionReadOnlyExecutionService, contractAddress, methodName);
        }


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
    }
}