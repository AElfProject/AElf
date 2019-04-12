using System;

namespace AElf.CrossChain.Grpc.Exceptions
{
    public class CertificateException : Exception
    {
        public CertificateException(string unableToLoadCertificate) : base(unableToLoadCertificate)
        {
        }
    }
}