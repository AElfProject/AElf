using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Google.Protobuf;

[assembly: InternalsVisibleTo("AElf.Kernel.Core.Tests")]
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
            if (bytes.Length != TypeConsts.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Address (sha256 of pubkey) bytes has to be {TypeConsts.AddressHashLength}. The input is {bytes.Length} bytes long.");
            }

            Value = ByteString.CopyFrom(bytes);
        }

        public static Address FromPublicKey(byte[] bytes)
        {
            var hash = TakeByAddressLength(SHA256.Create().ComputeHash(SHA256.Create().ComputeHash(bytes)));
            return new Address(hash);
        }


        //TODO: move this method into test project
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

        public static byte[] TakeByAddressLength(byte[] raw)
        {
            return raw.Take(TypeConsts.AddressHashLength).ToArray();
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
            if (_formattedAddress != null)
                return _formattedAddress;

            if (Value.Length != TypeConsts.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Serialized value does not represent a valid address. The input is {Value.Length} bytes long.");
            }

            var pubKeyHash = Base58CheckEncoding.Encode(Value.ToByteArray());

            return _formattedAddress = pubKeyHash;
        }


        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address FromBytes(byte[] bytes)
        {
            if (bytes.Length != TypeConsts.AddressHashLength)
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
            return new Address(Base58CheckEncoding.Decode(inputStr));
        }

        #endregion Load and dump
    }

    public class ChainAddress
    {
        public Address Address { get; }
        public int ChainId { get;}

        public ChainAddress(Address address, int chainId)
        {
            Address = address;
            ChainId = chainId;
        }

        public static ChainAddress Parse(string str)
        {
            var arr = str.Split('_');

            if (arr[0] != TypeConsts.AElfAddressPrefix)
            {
                throw new ArgumentException("invalid chain address", nameof(str));
            }

            var address = Address.Parse(arr[1]);

            var chainId = BitConverter.ToInt32(Base58CheckEncoding.Decode(arr[2]), 0);

            return new ChainAddress(address,chainId);
        }

        private string _formatted;

        public string GetFormatted()
        {
            if (_formatted != null)
                return _formatted;
            return _formatted = (TypeConsts.AElfAddressPrefix + "_") + Address.GetFormatted() +
                               ("_" + Base58CheckEncoding.Encode(ChainId.DumpByteArray()));
        }
    }
}