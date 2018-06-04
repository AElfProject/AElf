using AElf.Kernel.Crypto.ECDSA;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public interface ITransaction : IHashProvider, ISerializable
    {
        /// <summary>
        /// Method name
        /// </summary>
        string MethodName { get; set; }

        /// <summary>
        /// Params
        /// </summary>
        ByteString Params { get; set; }

        /// <summary>
        /// Fee
        /// </summary>
        ulong Fee { get; set; }
        
        ByteString P { get; set; }
        
        /// <summary>
        /// The caller
        /// </summary>
        Hash From { get; set; }

        /// <summary>
        /// The instrance of a smart contract
        /// </summary>
        Hash To { get; set; }

        ulong IncrementId { get; set; }
        
        ITransactionParallelMetaData GetParallelMetaData();

        /// <summary>
        /// return signature of tx
        /// </summary>
        /// <returns></returns>
        ECSignature GetSignature();

        /// <summary>
        /// return tx size
        /// </summary>
        /// <returns></returns>
        int Size();
    }

}