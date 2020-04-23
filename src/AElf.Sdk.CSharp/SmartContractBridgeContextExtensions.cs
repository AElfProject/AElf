using System.Collections.Generic;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using AElf.Kernel.SmartContract;


namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Extension methods that help with the interactions with the smart contract execution context.
    /// </summary>
    public static class SmartContractBridgeContextExtensions
    {
        /// <summary>
        /// Logs an event during the execution of a transaction. The event type is defined in the AElf.CSharp.core
        /// project.
        /// </summary>
        /// <param name="eventData">the event to log</param>
        /// <typeparam name="T">the type of the event</typeparam>
        public static void Fire<T>(this CSharpSmartContractContext context, T eventData) where T : IEvent<T>
        {
            context.FireLogEvent(eventData.ToLogEvent(context.Self));
        }

        /// <summary>
        /// Calls a method on another contract.
        /// </summary>
        /// <param name="address">the address of the contract you're seeking to interact with</param>
        /// <param name="methodName">the name of method you want to call</param>
        /// <param name="message">The protobuf message that will be the input to the call.</param>
        /// <typeparam name="T">The return type of the call</typeparam>
        /// <returns>The return value of the call.</returns>
        public static T Call<T>(this ISmartContractBridgeContext context, Address address,
            string methodName, IMessage message) where T : IMessage<T>, new()
        {
            return context.Call<T>(context.Self, address, methodName, ConvertToByteString(message));
        }

        /// <summary>
        /// Sends an inline transaction to another contract.
        /// </summary>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="message">The protobuf message that will be the input to the call.</param>
        public static void SendInline(this ISmartContractBridgeContext context, Address toAddress, string methodName,
            IMessage message)
        {
            context.SendInline(toAddress, methodName, ConvertToByteString(message));
        }

        /// <summary>
        /// Sends a virtual inline transaction to another contract.
        /// </summary>
        /// <param name="fromVirtualAddress">the virtual address to use as sender.</param>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="message">The protobuf message that will be the input to the call.</param>
        public static void SendVirtualInline(this ISmartContractBridgeContext context, Hash fromVirtualAddress,
            Address toAddress, string methodName, IMessage message)
        {
            context.SendVirtualInline(fromVirtualAddress, toAddress, methodName,
                ConvertToByteString(message));
        }

        /// <summary>
        /// Calls a method on another contract.
        /// </summary>
        /// <param name="address">the address of the contract you're seeking to interact with</param>
        /// <param name="methodName">the name of method you want to call</param>
        /// <param name="message">the protobuf message that will be the input to the call.</param>
        /// <typeparam name="T">the type of the return message.</typeparam>
        /// <returns>the result of the call.</returns>
        public static T Call<T>(this CSharpSmartContractContext context, Address address,
            string methodName, IMessage message) where T : IMessage<T>, new()
        {
            return context.Call<T>(context.Self, address, methodName, ConvertToByteString(message));
        }

        public static T Call<T>(this CSharpSmartContractContext context, Address fromAddress, Address toAddress,
            string methodName, IMessage message) where T : IMessage<T>, new()
        {
            return context.Call<T>(fromAddress, toAddress, methodName, ConvertToByteString(message));
        }

        public static T Call<T>(this CSharpSmartContractContext context, Address address,
            string methodName, ByteString message) where T : IMessage<T>, new()
        {
            return context.Call<T>(context.Self, address, methodName, message);
        }

        /// <summary>
        /// Sends a virtual inline transaction to another contract.
        /// </summary>
        /// <param name="fromVirtualAddress">the virtual address to use as sender.</param>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="message">The protobuf message that will be the input to the call.</param>
        public static void SendInline(this CSharpSmartContractContext context, Address toAddress, string methodName,
            IMessage message)
        {
            context.SendInline(toAddress, methodName, ConvertToByteString(message));
        }

        /// <summary>
        /// Sends a virtual inline transaction to another contract.
        /// </summary>
        /// <param name="fromVirtualAddress">the virtual address to use as sender.</param>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="message">The protobuf message that will be the input to the call.</param>
        public static void SendVirtualInline(this CSharpSmartContractContext context, Hash fromVirtualAddress,
            Address toAddress, string methodName, IMessage message)
        {
            context.SendVirtualInline(fromVirtualAddress, toAddress, methodName,
                ConvertToByteString(message));
        }

        /// <summary>
        /// Serializes a protobuf message to a protobuf ByteString. 
        /// </summary>
        /// <param name="message">the message to serialize.</param>
        /// <returns>ByteString.Empty if the message is null</returns>
        public static ByteString ConvertToByteString(IMessage message)
        {
            return message?.ToByteString() ?? ByteString.Empty;
            //return ByteString.CopyFrom(ParamsPacker.Pack(message));
        }
        
        public static Hash GenerateId(this ISmartContractBridgeContext @this, IEnumerable<byte> bytes)
        {
            return @this.GenerateId(@this.Self, bytes);
        }

        public static Hash GenerateId(this ISmartContractBridgeContext @this, string token)
        {
            return @this.GenerateId(@this.Self, token.GetBytes());
        }

        public static Hash GenerateId(this ISmartContractBridgeContext @this, Hash token)
        {
            return @this.GenerateId(@this.Self, token.Value);
        }

        public static Hash GenerateId(this ISmartContractBridgeContext @this)
        {
            return @this.GenerateId(@this.Self, null);
        }

        public static Hash GenerateId(this ISmartContractBridgeContext @this, Address address, Hash token)
        {
            return @this.GenerateId(address, token);
        }


        public static Address ConvertVirtualAddressToContractAddress(this ISmartContractBridgeContext @this,
            Hash virtualAddress)
        {
            return @this.ConvertVirtualAddressToContractAddress(virtualAddress, @this.Self);
        }

        public static Address ConvertVirtualAddressToContractAddressWithContractHashName(
            this ISmartContractBridgeContext @this, Hash virtualAddress)
        {
            return @this.ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress, @this.Self);
        }
    }
}