using System;
using Google.Protobuf;

namespace AElf.Types
{

    public partial class Address : ICustomDiagnosticMessage, IComparable<Address>
    {
        private string _formattedAddress;

        // Make private to avoid confusion
        private Address(byte[] bytes)
        {
            if (bytes.Length != AElfConstants.AddressHashLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            Value = ByteString.CopyFrom(bytes);
        }

        public int CompareTo(Address that)
        {
            if (that == null) throw new InvalidOperationException("Cannot compare address when address is null.");

            return CompareAddress(this, that);
        }

        /// <summary>
        ///     Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $@"""{ToBase58()}""";
        }

        // TODO: It should be an address generation method of KeyPair, instead of Address.FromPublicKey
        public static Address FromPublicKey(byte[] bytes)
        {
            var hash = bytes.ComputeHash().ComputeHash();
            return new Address(hash);
        }

        /// <summary>
        ///     Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Address FromBytes(byte[] bytes)
        {
            if (bytes.Length != AElfConstants.AddressHashLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            return new Address
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        /// <summary>
        ///     Dumps the content value to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return Value.ToByteArray();
        }

        /// <summary>
        ///     Construct address from base58 encoded string.
        /// </summary>
        /// <param name="inputStr"></param>
        /// <returns></returns>
        public static Address FromBase58(string inputStr)
        {
            return FromBytes(Base58CheckEncoding.Decode(inputStr));
        }

        /// <summary>
        ///     Converts address into base58 representation.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string ToBase58()
        {
            if (_formattedAddress != null)
                return _formattedAddress;

            if (Value.Length != AElfConstants.AddressHashLength)
                throw new ArgumentException("Invalid address", nameof(Value));

            var pubKeyHash = Base58CheckEncoding.Encode(Value.ToByteArray());
            return _formattedAddress = pubKeyHash;
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
                return address2 == null ? 1 : ByteStringHelper.Compare(address1.Value, address2.Value);

            if (address2 == null) return 0;

            return -1;
        }
    }

    public class ChainAddress
    {
        private string _formatted;

        public ChainAddress(Address address, int chainId)
        {
            Address = address;
            ChainId = chainId;
        }

        public Address Address { get; }
        public int ChainId { get; }

        public static ChainAddress Parse(string chainAddressString, string symbol)
        {
            var arr = chainAddressString.Split('_');
            if (arr[0] != symbol) throw new ArgumentException("invalid chain address", nameof(chainAddressString));

            var address = Address.FromBase58(arr[1]);
            var chainId = Base58CheckEncoding.Decode(arr[2]).ToInt32(false);

            return new ChainAddress(address, chainId);
        }

        public string GetFormatted(string addressPrefix, int chainId)
        {
            if (_formatted != null) return _formatted;
            var addressSuffix = Base58CheckEncoding.Encode(chainId.ToBytes(false));
            _formatted = $"{addressPrefix}_{Address.ToBase58()}_{addressSuffix}";
            return _formatted;
        }
    }
}