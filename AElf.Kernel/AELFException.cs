using System;

namespace AElf.Kernel
{
    public class AELFException : ApplicationException
    {
        public AELFException(string msg) : base(msg) { }
    }
}
