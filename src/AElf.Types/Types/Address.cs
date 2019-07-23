using System;
using System.Runtime.CompilerServices;
using Google.Protobuf;

[assembly: InternalsVisibleTo("AElf.Kernel.Core.Tests")]
[assembly: InternalsVisibleTo("AElf.Contracts.SideChain.Tests")]
[assembly: InternalsVisibleTo("AElf.Contracts.Authorization.Tests")]

namespace AElf.Types
{
    public partial class Address : ICustomDiagnosticMessage, IComparable<Address>
    {
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $@"""{GetFormatted()}""";
        }

        // Make private to avoid confusion
        private Address(byte[] bytes)
        {
            if (bytes.Length != TypeConsts.AddressHashLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            Value = ByteString.CopyFrom(bytes);
        }

        public static Address FromPublicKey(byte[] bytes)
        {
            var hash = bytes.ComputeHash().ComputeHash();
            return new Address(hash);
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Address FromBytes(byte[] bytes)
        {
            if (bytes.Length != TypeConsts.AddressHashLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            return new Address
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        /// <summary>
        /// Dumps the content value to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return Value.ToByteArray();
        }

        public int CompareTo(Address that)
        {
            if (that == null)
            {
                throw new InvalidOperationException("Cannot compare address when address is null.");
            }

            return CompareAddress(this, that);
        }

        public static bool operator ==(Address address1, Address address2)
        {
            return address1?.Equals(address2) ?? ReferenceEquals(address2, null);
        }

        public static bool operator !=(Address address1, Address address2)
        {
            return !(address1 == address2);
        }

        public static bool operator <(Address address1, Address address2)
        {
            return CompareAddress(address1, address2) < 0;
        }

        public static bool operator >(Address address1, Address address2)
        {
            return CompareAddress(address1, address2) > 0;
        }

        private static int CompareAddress(Address address1, Address address2)
        {
            if (address1 != null)
            {
                return address2 == null ? 1 : ByteStringHelper.Compare(address1.Value, address2.Value);
            }

            if (address2 == null)
            {
                return 0;
            }

            return -1;
        }

        private string _formattedAddress;

        public string GetFormatted()
        {
            if (_formattedAddress != null)
                return _formattedAddress;

            if (Value.Length != TypeConsts.AddressHashLength)
                throw new ArgumentException("Invalid address", nameof(Value));

            var pubKeyHash = Base58CheckEncoding.Encode(Value.ToByteArray());
            return _formattedAddress = pubKeyHash;
        }
    }

    public class ChainAddress
    {
        public Address Address { get; }
        public int ChainId { get; }

        public ChainAddress(Address address, int chainId)
        {
            Address = address;
            ChainId = chainId;
        }

        public static ChainAddress Parse(string chainAddressString, string symbol)
        {
            var arr = chainAddressString.Split('_');

            if (arr[0] != symbol)
            {
                throw new ArgumentException("invalid chain address", nameof(chainAddressString));
            }

            var address = AddressHelper.Base58StringToAddress(arr[1]);

            var chainId = BitConverter.ToInt32(Base58CheckEncoding.Decode(arr[2]), 0);

            return new ChainAddress(address, chainId);
        }

        private string _formatted;

        public string GetFormatted(string addressPrefix, int chainId)
        {
            if (_formatted != null)
                return _formatted;
            return _formatted = (addressPrefix + "_") + Address.GetFormatted() +
                                ("_" + Base58CheckEncoding.Encode(chainId.DumpByteArray()));
        }
    }
}