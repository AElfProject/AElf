using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using AElf.WebApp.Application.Chain.AppServices;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppSendTransactionService : IApplicationService
    {
        ILocalEventBus LocalEventBus { get; set; }

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
    }

    /// <summary>
    /// send transaction services
    /// </summary>
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppSendTransactionService" />
    public sealed class AppSendTransactionService : IAppSendTransactionService
    {
        public ILocalEventBus LocalEventBus { get; set; }

        private readonly IAppContractService _appContractService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSendTransactionService"/> class.
        /// </summary>
        /// <param name="appContractService">The application contract service.</param>
        public AppSendTransactionService(IAppContractService appContractService)
        {
            LocalEventBus = NullLocalEventBus.Instance;
            _appContractService = appContractService;
        }

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
                await _appContractService.GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);

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
                    await _appContractService.GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
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
    }
}