using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Base58Check;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

[assembly: InternalsVisibleTo("AElf.Kernel.Tests")]
[assembly: InternalsVisibleTo("AElf.Contracts.SideChain.Tests")]
[assembly: InternalsVisibleTo("AElf.Contracts.Authorization.Tests")]

namespace AElf.Common
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
            if (bytes.Length != GlobalConfig.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Address (sha256 of pubkey) bytes has to be {GlobalConfig.AddressHashLength}. The input is {bytes.Length} bytes long.");
            }

            Value = ByteString.CopyFrom(bytes);
        }

        /// <summary>
        /// Creates an address from raw byte array. If the byte array is longer than required address length,
        /// the first bytes will be skipped. The input byte array is usually serialized uncompressed public key.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
//        public static Address FromRawBytes(byte[] bytes)
//        {
//            return new Address(bytes);
//        }
        
        public static Address FromPublicKey(byte[] chainId, byte[] bytes)
        {
            var hash = TakeByAddressLength(SHA256.Create().ComputeHash(SHA256.Create().ComputeHash(bytes)));
            return new Address(hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address BuildContractAddress(byte[] chainId, ulong serialNumber)
        {
            var hash = Hash.FromTwoHashes(Hash.LoadByteArray(chainId), Hash.FromRawBytes(serialNumber.ToBytes()));
            return new Address(TakeByAddressLength(hash.DumpByteArray()));
        }

        public static Address BuildContractAddress(Hash chainId, ulong serialNumber)
        {
            return BuildContractAddress(chainId.DumpByteArray(), serialNumber);
        }

        /// <summary>
        /// Creates an address from a string. This method is supposed to be used for test only.
        /// The hash bytes of the string will be used to create the address.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Address FromString(string name)
        {
            return new Address(TakeByAddressLength(name.CalculateHash()));
        }

        /// <summary>
        /// Only used in tests to generate random addresses.
        /// </summary>
        /// <returns></returns>
        public static Address Generate()
        {
            return new Address(TakeByAddressLength(Guid.NewGuid().ToByteArray().CalculateHash()));
        }

        /// <summary>
        /// Only used in tests to generate random addresses.
        /// </summary>
        /// <returns></returns>
        public static Address Generate(byte[] chainId)
        {
            return Generate();
        }

        public static byte[] TakeByAddressLength(byte[] raw)
        {
            return raw.Take(GlobalConfig.AddressHashLength).ToArray();
        }

        #region Predefined

        public static readonly Address AElf = FromString("AElf");

        public static readonly Address Zero = new Address(TakeByAddressLength(new byte[] { }.CalculateHash()));

        public static readonly Address Genesis = FromString("Genesis");
        
        #endregion

        #region Comparing

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
                return address2 == null ? 1 : Compare(address1, address2);
            }

            if (address2 == null)
            {
                return 0;
            }

            return -1;
        }

        private static int Compare(Address x, Address y)
        {
            if (x == null || y == null)
            {
                throw new InvalidOperationException("Cannot compare address when address is null");
            }

            return ByteStringHelpers.Compare(x.Value, y.Value);
        }

        public int CompareTo(Address that)
        {
            return Compare(this, that);
        }

        #endregion

        #region Load and dump

        /// <summary>
        /// Dumps the content value to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] DumpByteArray()
        {
            return Value.ToByteArray();
        }

        private string _formattedAddress;
        public string GetFormatted()
        {
            if (Value.Length != GlobalConfig.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Serialized value does not represent a valid address. The input is {Value.Length} bytes long.");
            }

            string pubKeyHash = Base58CheckEncoding.Encode(Value.ToByteArray());
            
            return string.IsNullOrEmpty(_formattedAddress) 
                ? (_formattedAddress = GlobalConfig.AElfAddressPrefix + '_' + pubKeyHash) : _formattedAddress;
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address FromBytes(byte[] bytes)
        {
            if (bytes.Length != GlobalConfig.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Input value does not represent a valid address. The input is {bytes.Length} bytes long.");
            }
            return new Address
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        public static Address Parse(string inputStr)
        {
            string[] split = inputStr.Split('_');

            if (split.Length < 2)
                return null;

            if (String.CompareOrdinal(split.First(), "ELF") != 0)
                return null;
            
            return new Address(Base58CheckEncoding.Decode(split.Last()));
        }
        
        #endregion Load and dump
    }
}