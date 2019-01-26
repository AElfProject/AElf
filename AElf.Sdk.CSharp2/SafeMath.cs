namespace AElf.Sdk.CSharp
{
    public static class SafeMath
    {
        #region int

        public static int Mul(this int a, int b)
        {
            checked
            {
                return a * b;
            }
        }

        public static int Div(this int a, int b)
        {
            checked
            {
                return a / b;    
            }
        }

        public static int Sub(this int a, int b)
        {
            checked
            {
                return a - b;
            }
        }

        public static int Add(this int a, int b)
        {
            checked
            {
                return a + b;
            }
        }        

        #endregion int
        
        #region uint

        public static uint Mul(this uint a, uint b)
        {
            checked
            {
                return a * b;
            }
        }

        public static uint Div(this uint a, uint b)
        {
            checked
            {
                return a / b;    
            }
        }

        public static uint Sub(this uint a, uint b)
        {
            checked
            {
                return a - b;
            }
        }

        public static uint Add(this uint a, uint b)
        {
            checked
            {
                return a + b;
            }
        }        

        #endregion uint
        
        #region long

        public static long Mul(this long a, long b)
        {
            checked
            {
                return a * b;
            }
        }

        public static long Div(this long a, long b)
        {
            checked
            {
                return a / b;
            }
        }

        public static long Sub(this long a, long b)
        {
            checked
            {
                return a - b;
            }
        }

        public static long Add(this long a, long b)
        {
            checked
            {
                return a + b;
            }
        }        

        #endregion long
        
        #region ulong

        public static ulong Mul(this ulong a, ulong b)
        {
            checked
            {
                return a * b;
            }
        }

        public static ulong Div(this ulong a, ulong b)
        {
            checked
            {
                return a / b;    
            }
        }

        public static ulong Sub(this ulong a, ulong b)
        {
            checked
            {
                return a - b;    
            }
        }

        public static ulong Add(this ulong a, ulong b)
        {
            checked
            {
                return a + b;
            }
        }        

        #endregion ulong

    }
}