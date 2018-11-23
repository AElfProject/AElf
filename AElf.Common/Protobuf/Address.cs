using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Base58Check;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

[assembly: InternalsVisibleTo("AElf.Kernel.Tests")]
[assembly: InternalsVisibleTo("AElf.Contracts.SideChain.Tests")]

namespace AElf.Common
{
    public partial class Address : ICustomDiagnosticMessage, IComparable<Address>
    {
        public static readonly byte[] _fakeChainId = {0x01, 0x02, 0x03};
        
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $@"""{GetFormatted()}""";
        }

        // Make private to avoid confusion
        private Address(byte[] chainId, byte[] bytes)
        {
            if (bytes.Length != GlobalConfig.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Address (sha256 of pubkey) bytes has to be {GlobalConfig.AddressHashLength}. The input is {bytes.Length} bytes long.");
            }

            if (chainId.Length != GlobalConfig.ChainIdLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"The chain id length has to be {GlobalConfig.ChainIdLength}. The input is {bytes.Length} bytes long.");
            }

            Value = ByteString.CopyFrom(ByteArrayHelpers.Combine(chainId, bytes));
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
            var hash = SHA256.Create().ComputeHash(SHA256.Create().ComputeHash(bytes));
            return new Address(chainId, hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address BuildContractAddress(byte[] chainId, ulong serialNumber)
        {
            var hash = Hash.FromTwoHashes(Hash.LoadByteArray(chainId), Hash.FromRawBytes(new UInt64Value{ Value = serialNumber}.ToByteArray()));
            return new Address(chainId, hash.DumpByteArray());
        }

        /// <summary>
        /// Creates an address from a string. This method is supposed to be used for test only.
        /// The hash bytes of the string will be used to create the address.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Address FromString(string name)
        {
            return new Address( _fakeChainId, name.CalculateHash());
        }

        /// <summary>
        /// Only used in tests to generate random addresses.
        /// </summary>
        /// <returns></returns>
        public static Address Generate()
        {
            return new Address(_fakeChainId, Guid.NewGuid().ToByteArray().CalculateHash());
        }
        
        #region Predefined

        public static readonly Address AElf = FromString("AElf");

        public static readonly Address Zero = new Address( _fakeChainId, new byte[] { }.CalculateHash());

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
            if (Value.Length != GlobalConfig.AddressHashLength + GlobalConfig.ChainIdLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Serialized value does not represent a valid address. The input is {Value.Length} bytes long.");
            }

            string chainId = Base58CheckEncoding.EncodePlain(Value.Take(3).ToArray());
            string pubKeyHash = Base58CheckEncoding.Encode(Value.Skip(3).ToArray());
            
            return string.IsNullOrEmpty(_formattedAddress) 
                ? (_formattedAddress = GlobalConfig.AElfAddressPrefix + '_' + chainId + '_' + pubKeyHash) : _formattedAddress;
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address FromBytes(byte[] bytes)
        {
            return new Address
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        public static Address Parse(string inputStr)
        {
            string[] split = inputStr.Split('_');

            if (split.Length != 3)
                return null;

            if (String.CompareOrdinal(split[0], "ELF") != 0)
                return null;

            if (split[1].Length != 4)
                return null;
            
            return new Address(Base58CheckEncoding.DecodePlain(split[1]), Base58CheckEncoding.Decode(split[2]));
        }
        
        #endregion Load and dump
    }
}