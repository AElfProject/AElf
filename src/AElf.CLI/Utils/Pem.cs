using System;
using System.IO;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace AElf.CLI.Utils
{
    public class Pem
    {
        private static readonly SecureRandom Random = new SecureRandom();
        const string Algo = "AES-256-CFB";

        public static class ECParameters
        {
            //public static SecureRandom SecureRandom = new SecureRandom();
            public static SecureRandom SecureRandom = new SecureRandom();
            public static X9ECParameters Curve = SecNamedCurves.GetByName("secp256k1");

            public static ECDomainParameters DomainParams =
                new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H);
        }

        private class Password : IPasswordFinder
        {
            private readonly char[] _password;

            public Password(char[] word)
            {
                _password = (char[]) word.Clone();
            }

            public char[] GetPassword()
            {
                return (char[]) _password.Clone();
            }
        }

        private static string ToHex(byte[] bytes)
        {
            int offset = 0;
            int length = bytes.Length * 2 + offset;
            char[] c = new char[length];

            byte b;

            for (int bx = 0, cx = offset; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte) (bytes[bx] >> 4));
                c[cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte) (bytes[bx] & 0x0F));
                c[++cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        private static byte[] FromHexString(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];

            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        public static string ReadPrivateKey(string filePath, string password)
        {
            AsymmetricCipherKeyPair cipherKeyPair;
            using (var textReader = File.OpenText(filePath))
            {
                PemReader pr = new PemReader(textReader, new Password(password.ToCharArray()));
                cipherKeyPair = pr.ReadObject() as AsymmetricCipherKeyPair;
            }

            if (cipherKeyPair == null)
                return null;
            ECPrivateKeyParameters newPrivateKeyParam = (ECPrivateKeyParameters) cipherKeyPair.Private;
            return newPrivateKeyParam.D.ToByteArrayUnsigned().ToHex();
        }

        public static bool WriteKeyPair(string filePath, string privateKey, string publicKey, string password)
        {
            var skParams = new ECPrivateKeyParameters(new BigInteger(privateKey, 16), ECParameters.DomainParams);

            var pkPoint = ECParameters.Curve.Curve.DecodePoint(FromHexString(publicKey));
            var pkParams = new ECPublicKeyParameters(pkPoint, ECParameters.DomainParams);

            var akp = new AsymmetricCipherKeyPair(pkParams, skParams);

            using (var writer = File.CreateText(filePath))
            {
                var pw = new PemWriter(writer);
                pw.WriteObject(akp, Algo, password.ToCharArray(), Random);
                pw.Writer.Close();
            }

            Console.WriteLine($@"Account info has been saved to ""{filePath}""");
            return true;
        }
    }
}