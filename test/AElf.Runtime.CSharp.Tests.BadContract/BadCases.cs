using System;
using System.Collections.ObjectModel;
using System.Linq;
using Google.Protobuf.Reflection;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public class BadCase1
    {
        public static FileDescriptor Descriptor
        {
            get { return descriptor; }
        }

        private static FileDescriptor descriptor;

        public void SetFileDescriptor()
        {
            descriptor = null;
        }
    }

    public class BadCase2
    {
        public int I;
        public static int Number = 1;
    }

    public class BadCase3
    {
        public static readonly BadCase2 field;
    }
    
    // Similar to Linq generated class but with instance field I, should not be allowed
    public class BadCase4
    {
        public static readonly BadCase4 field;

        public int I;
    }

    public static class BadCase5
    {
        static readonly ReadOnlyCollection<BadCase3> collection;

        static BadCase5()
        {
            collection = Array.AsReadOnly(new[] { new BadCase3() });
        }
    }

    public class BadCase6
    {
        public BadCase6()
        {
            var array = new int[0][]; // multi dim array
            int length = array.Length;
        }
    }
}