using System;

namespace Solang
{
    public class SelectorNotFoundException : Exception
    {
        public SelectorNotFoundException() : base()
        {

        }

        public SelectorNotFoundException(string message) : base(message)
        {

        }
    }
}