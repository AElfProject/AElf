using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Sdk.CSharp
{
    public interface IContext
    {
        int ChainId { get; }

        void LogDebug(Func<string> func);

        void FireEvent<TEvent>(TEvent logEvent) where TEvent : Event;
        Hash TransactionId { get; }
        
        // TODO: Remove Transaction
        Transaction Transaction { get; }
        Address Sender { get; }

        Address Self { get; }

        // TODO: Remove genesis
        Address Genesis { get; }
        long CurrentHeight { get; }

        DateTime CurrentBlockTime { get; }
        Hash PreviousBlockHash { get; }

        // TODO: Remove RecoverPublicKey(byte[] signature, byte[] hash)
        byte[] RecoverPublicKey(byte[] signature, byte[] hash);

        byte[] RecoverPublicKey();

        // TODO: Remove GetBlockByHeight
        Block GetPreviousBlock();

        bool VerifySignature(Transaction tx);

        /// <summary>
        /// Generate txn not executed before next block. 
        /// </summary>
        /// <param name="deferredTxn"></param>
        void SendDeferredTransaction(Transaction deferredTxn);

        void DeployContract(Address address, SmartContractRegistration registration, Hash name);

        void UpdateContract(Address address, SmartContractRegistration registration, Hash name);
//        Hash ChainId { get; }
//        Address ContractZeroAddress { get; }
//
//        Address CrossChainContractAddress { get; }
//        Address AuthorizationContractAddress { get; }
//        Address ResourceContractAddress { get; }
//
//        Address TokenContractAddress { get; }
//        Address ConsensusContractAddress { get; }
//        Address DividendsContractAddress { get; }
//        Address Genesis { get; }
//
//        void DeployContract(Address address, SmartContractRegistration registration);
//
//        Task DeployContractAsync(Address address, SmartContractRegistration registration);
//
//        Task UpdateContractAsync(Address address, SmartContractRegistration registration);
//
//        Hash GetPreviousBlockHash();
//
//        ulong GetCurrentHeight();
//
//        Address GetContractAddress();
//
//        byte[] RecoverPublicKey(byte[] signature, byte[] hash);
//
//        byte[] RecoverPublicKey();
//
//        Miners GetMiners();
//
//        ulong GetCurrentRoundNumber();
//
//        ulong GetCurrentTermNumber();
//
//        TermSnapshot GetTermSnapshot(ulong termNumber);
//
//        Address GetContractOwner();
//
//        Transaction GetTransaction();
//
//        Hash GetTxnHash();
//
//        Address GetFromAddress();
//
//        Address GetToAddress();
//
//        ulong GetResourceBalance(Address address, ResourceType resourceType);
//
//        ulong GetTokenBalance(Address address);
//
//        void SendInline(Address contractAddress, string methodName, params object[] args);
//
//        void SendDividends(params object[] args);
//
//        void SendInlineByContract(Address contractAddress, string methodName, params object[] args);
//
//        bool Call(Address contractAddress, string methodName, params object[] args);
//
//        byte[] GetCallResult();
//
//        bool VerifySignature(Transaction proposedTxn);
//
//        bool VerifyTransaction(Hash txId, MerklePath merklePath, ulong parentChainHeight);
//
//        void LockToken(ulong amount);
//
//        void UnlockToken(Address address, ulong amount);
//
//        void LockResource(ulong amount, ResourceType resourceType);
//
//        void WithdrawResource(ulong amount, ResourceType resourceType);
//
//        void Assert(bool asserted, string message = "Assertion failed!");
//
//        void Equal<T>(T expected, T actual, string message = "Assertion failed!");
//
//        void FireEvent(LogEvent logEvent);
//
//        void SendDeferredTransaction(Transaction deferredTxn);
//
//        void CheckAuthority(Address fromAddress = null);
//
//        void IsMiner(string err);
//
//        Hash Propose(string proposalName, double waitingPeriod, Address targetAddress, string invokingMethod,
//            params object[] args);
    }
}