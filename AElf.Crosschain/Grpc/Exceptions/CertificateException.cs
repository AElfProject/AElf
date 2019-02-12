using System;

namespace AElf.Crosschain.Exceptions
{
    public class CertificateException : Exception
    {
        public CertificateException(string unableToLoadCertificate) : base(unableToLoadCertificate)
        {
        }
    }
}