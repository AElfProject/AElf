using System;

namespace AElf.Contracts.TestContract.BasicSecurity
{
    public class BasicContractTestType
    {
        private int _number;
        private static int _testTypeStaticNumber;
        private static Func<string, string> _str;
        private TestType _testType;

        public BasicContractTestType()
        {
            _testType = new TestType();
        }

        public void SetBasicContractTestType(int number)
        {
            _number = number;
            _testTypeStaticNumber = number;
            _str = s => s.ToLower();
            _testType = new TestType(number);
        }
        
        public int CheckNumberValue()
        {
            return _number;
        }
            
        public int CheckStaticNumberValue()
        {
            return _testTypeStaticNumber;
        }
        
        public Func<string, string> CheckFunc()
        {
            return _str;
        }
        
        public TestType CheckTypeValue()
        {
            return _testType;
        }

        public class TestType
        {
            private readonly int _typeNumber;
            private const int TypeConstNumber = 1;

            public TestType()
            {
            }
            public TestType(int typeNumber)
            {
                _typeNumber = typeNumber;
            }
            
            public int CheckNumberValue()
            {
                return _typeNumber;
            }
            
            public int CheckConstNumberValue()
            {
                return TypeConstNumber;
            }
        }
    }
}