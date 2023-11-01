using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public class WebAssemblySmartSmartContractContext : CSharpSmartContractContext, IWebAssemblySmartContractContext
{
    public WebAssemblySmartSmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation) : base(
        smartContractBridgeContextImplementation)
    {
        SmartContractBridgeContextImplementation = smartContractBridgeContextImplementation;
    }

    public ISmartContractBridgeContext SmartContractBridgeContextImplementation { get; }

    /// <summary>
    ///     Calls a method on another contract.
    /// </summary>
    /// <param name="fromAddress">The address to use as sender.</param>
    /// <param name="toAddress">The address of the contract you're seeking to interact with.</param>
    /// <param name="methodName">The name of method you want to call.</param>
    /// <param name="args">
    ///     The input arguments for calling that method. This is usually generated from the protobuf
    ///     definition of the input type
    /// </param>
    /// <typeparam name="T">The type of the return message.</typeparam>
    /// <returns>The result of the call.</returns>
    public byte[] CallMethod(Address fromAddress, Address toAddress, string methodName, ByteString args)
    {
        return ((HostSmartContractBridgeContext)SmartContractBridgeContextImplementation).CallMethod(fromAddress,
            toAddress, methodName, args);
    }

    public byte[] DelegateCall(Address fromAddress, Address toAddress, string methodName, ByteString args)
    {
        return ((HostSmartContractBridgeContext)SmartContractBridgeContextImplementation).DelegateCall(fromAddress,
            toAddress, methodName, args);
    }
}