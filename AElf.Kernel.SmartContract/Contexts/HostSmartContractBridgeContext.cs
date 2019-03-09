using System;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Contexts
{
    public class HostSmartContractBridgeContext : IHostSmartContractBridgeContext
    {
        private readonly ISmartContractBridgeService _smartContractBridgeService;

        public HostSmartContractBridgeContext(ISmartContractBridgeService smartContractBridgeService)
        {
            _smartContractBridgeService = smartContractBridgeService;
        }

        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext SmartContractContext { get; set; }

        public Address GetContractAddressByName(Hash hash)
        {
            return _smartContractBridgeService.GetAddressByContractName(hash);
        }

        public int ChainId => _smartContractBridgeService.GetChainId();

        public void LogDebug(Func<string> func)
        {
#if DEBUG
            _smartContractBridgeService.LogDebug(() =>
                $"TX = {Transaction.GetHash().ToHex()}, Method = {Transaction.MethodName}, {func()}");
#endif
        }

        public void FireLogEvent(LogEvent logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent);
        }


        public Transaction Transaction => TransactionContext.Transaction.Clone();
        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.Clone();
        public Address Self => SmartContractContext.ContractAddress.Clone();
        public Address Genesis => Address.Genesis;
        public long CurrentHeight => TransactionContext.BlockHeight;
        public DateTime CurrentBlockTime => TransactionContext.CurrentBlockTime;
        public Hash PreviousBlockHash => TransactionContext.PreviousBlockHash.Clone();

        public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            var cabBeRecovered = CryptoHelpers.RecoverPublicKey(signature, hash, out var publicKey);
            return !cabBeRecovered ? null : publicKey;
        }

        /// <summary>
        /// Recovers the first public key signing this transaction.
        /// </summary>
        /// <returns>Public key byte array</returns>
        public byte[] RecoverPublicKey()
        {
            return RecoverPublicKey(TransactionContext.Transaction.Sigs.First().ToByteArray(),
                TransactionContext.Transaction.GetHash().DumpByteArray());
        }

        public void SendInline(Address address, string methodName, params object[] args)
        {
            TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = Self,
                To = address,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }


        public Block GetPreviousBlock()
        {
            return AsyncHelper.RunSync(() => _smartContractBridgeService.GetBlockByHashAsync(
                TransactionContext.PreviousBlockHash));
        }

        public bool VerifySignature(Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1 && tx.Type != TransactionType.MsigTransaction)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(),
                    tx.GetHash().DumpByteArray(), out var pubKey);
                return canBeRecovered && Address.FromPublicKey(pubKey).Equals(tx.From);
            }

            return true;
        }

        public void SendDeferredTransaction(Transaction deferredTxn)
        {
            TransactionContext.Trace.DeferredTransaction = deferredTxn.ToByteString();
        }

        public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
        {
            //TODO: only check it in sdk not safe, we should check the security in the implement, in the 
            //method SmartContractContext.DeployContract or it's service 
            if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress()))
            {
                throw new NoPermissionException();
            }

            AsyncHelper.RunSync(() => _smartContractBridgeService.DeployContractAsync(address, registration,
                false, name));
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress()))
            {
                throw new NoPermissionException();
            }

            AsyncHelper.RunSync(() => _smartContractBridgeService.DeployContractAsync(address, registration,
                false, null));
        }
    }
}