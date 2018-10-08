using System;
using System.Linq;
using AElf.Common.Extensions;
using Google.Protobuf;

namespace AElf.Common
{
    public partial class Address : ICustomDiagnosticMessage
    {
        public string ToDiagnosticString()
        {
            return $@"""{Dumps()}""";
        }

        private Address(byte[] bytes)
        {
            if (bytes.Length < GlobalConfig.AddressLength)
            {
                throw new ArgumentOutOfRangeException($"Address bytes has to be at least {GlobalConfig.AddressLength}. The input is {bytes.Length} bytes long.");
            }
            var toTruncate = bytes.Length - GlobalConfig.AddressLength;
            Value = ByteString.CopyFrom(bytes.Skip(toTruncate).ToArray());
        }

        public static Address FromBytes(byte[] bytes)
        {
            return new Address(bytes);
        }
        
        public static Address FromString(string name)
        {
            return new Address(name.CalculateHash());
        }

//        // ReSharper disable once InconsistentNaming
//        public static Address FromECKeyPair(ECKeyPair keyPair)
//        {
//            return new Address(keyPair.GetEncodedPublicKey());
//        }
        
        public static readonly Address AElf = FromString("AElf");

        public static readonly Address Zero = new Address(new byte[] { }.CalculateHash());

        public static readonly Address Genesis = FromString("Genesis");

        public byte[] GetValueBytes() => Value.ToByteArray();

        public string Dumps()
        {
            return Value.ToByteArray().ToHex();
        }

        public static Address Loads(string hex)
        {
            var bytes = ByteArrayHelpers.FromHexString(hex);
            if (bytes.Length != GlobalConfig.AddressLength)
            {
                throw new ArgumentOutOfRangeException(nameof(hex));
            }
            return new Address()
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }
    }
}