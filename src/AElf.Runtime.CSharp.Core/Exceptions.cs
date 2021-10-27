﻿using System;

namespace AElf.Runtime.CSharp.Core
{
    public class InvalidMethodNameException : Exception
    {
        public InvalidMethodNameException() : base()
        {
        }

        public InvalidMethodNameException(string message) : base(message)
        {
        }
    }

    public class RuntimeException : Exception
    {
        public RuntimeException() : base()
        {
        }

        public RuntimeException(string message) : base(message)
        {
        }
    }
}
