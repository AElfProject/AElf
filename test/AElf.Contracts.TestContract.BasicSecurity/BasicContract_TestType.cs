using System;

namespace AElf.Contracts.TestContract.BasicSecurity
{
    public class BasicContractTestType
    {
        private int _number = 1;
        private static int _testTypeStaticNumber;
        private static Func<string, string> _strFunc;

        private static InnerTestType _innerTypePrivateStaticField;
        private static BasicContractTestType _basicContractTestTypePrivateStaticField;
        public static BasicContractTestType BasicContractTestTypePublicStaticField;
        

        public void SetBasicContractTestType(int number)
        {
            _number = number;
            _testTypeStaticNumber = number;
            _strFunc = s => s.ToLower();
            _innerTypePrivateStaticField = new InnerTestType(number);
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
            return _strFunc;
        }

        public InnerTestType CheckTypeValue()
        {
            return _innerTypePrivateStaticField;
        }

        public void SetStaticField()
        {
            _basicContractTestTypePrivateStaticField = this;
        }
        
        public static bool CheckAllStaticFieldsReset()
        {
            return _strFunc == null && _innerTypePrivateStaticField == null &&
                   _basicContractTestTypePrivateStaticField == null &&
                   BasicContractTestTypePublicStaticField == null;
        }

        public class InnerTestType
        {
            private readonly int _typeNumber;
            private const int TypeConstNumber = 1;

            private static InnerTestType _innerTestTypePrivateStaticField;
            public static InnerTestType InnerTestTypePublicStaticField;
            

            public InnerTestType(int typeNumber)
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
            
            public void SetStaticField()
            {
                _innerTestTypePrivateStaticField = this;
            }
            
            public static bool CheckInnerTypeStaticFieldsReset()
            {
                return _innerTestTypePrivateStaticField == null && InnerTestTypePublicStaticField == null;
            }
        }
    }
}