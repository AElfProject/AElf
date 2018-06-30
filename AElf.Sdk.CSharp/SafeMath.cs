namespace AElf.Sdk.CSharp
{
    public static class SafeMath
    {
        #region int

        public static int Mul(this int a, int b)
        {
            if (a == 0)
            {
                return 0;
            }

            var c = a * b;
            Api.Assert(c / a == b);
            return c;
        }

        public static int Div(this int a, int b)
        {
            return a / b;
        }

        public static int Sub(this int a, int b)
        {
            Api.Assert(b <= a);
            return a - b;
        }

        public static int Add(this int a, int b)
        {
            var c = a + b;
            Api.Assert(c >= a);
            return c;
        }        

        #endregion int
        
        #region uint

        public static uint Mul(this uint a, uint b)
        {
            if (a == 0)
            {
                return 0;
            }

            var c = a * b;
            Api.Assert(c / a == b);
            return c;
        }

        public static uint Div(this uint a, uint b)
        {
            return a / b;
        }

        public static uint Sub(this uint a, uint b)
        {
            Api.Assert(b <= a);
            return a - b;
        }

        public static uint Add(this uint a, uint b)
        {
            var c = a + b;
            Api.Assert(c >= a);
            return c;
        }        

        #endregion uint
        
        #region long

        public static long Mul(this long a, long b)
        {
            if (a == 0)
            {
                return 0;
            }

            var c = a * b;
            Api.Assert(c / a == b);
            return c;
        }

        public static long Div(this long a, long b)
        {
            return a / b;
        }

        public static long Sub(this long a, long b)
        {
            Api.Assert(b <= a);
            return a - b;
        }

        public static long Add(this long a, long b)
        {
            var c = a + b;
            Api.Assert(c >= a);
            return c;
        }        

        #endregion long
        
        #region ulong

        public static ulong Mul(this ulong a, ulong b)
        {
            if (a == 0)
            {
                return 0;
            }

            var c = a * b;
            Api.Assert(c / a == b);
            return c;
        }

        public static ulong Div(this ulong a, ulong b)
        {
            return a / b;
        }

        public static ulong Sub(this ulong a, ulong b)
        {
            Api.Assert(b <= a);
            return a - b;
        }

        public static ulong Add(this ulong a, ulong b)
        {
            var c = a + b;
            Api.Assert(c >= a);
            return c;
        }        

        #endregion ulong

    }
}