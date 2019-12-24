using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public class BadContract : BadContractContainer.BadContractBase
    {
        public override Empty UpdateDoubleState(DoubleInput input)
        {
            State.Double.Value = input.DoubleValue;

            return new Empty();
        }

        public override Empty UpdateFloatState(FloatInput input)
        {
            State.Float.Value = input.FloatValue;
            
            return new Empty();
        }

        public override RandomOutput UpdateStateWithRandom(Empty input)
        {
            var random = new Random().Next();

            State.CurrentRandom.Value = random;
            
            return new RandomOutput()
            {
                RandomValue = random
            };
        }

        public override DateTimeOutput UpdateStateWithCurrentTime(Empty input)
        {
            var current = DateTime.Now;

            State.CurrentTime.Value = current;

            State.CurrentTimeUtc.Value = DateTime.UtcNow;

            State.CurrentTimeToday.Value = DateTime.Today;

            return new DateTimeOutput()
            {
                DateTimeValue = Timestamp.FromDateTime(current)
            };
        }

        public override Empty WriteFileToNode(FileInfoInput input)
        {
            using (var writer = new StreamWriter(input.FilePath))
            {
                writer.Write(input.FileContent);
            }
            
            return new Empty();
        }

        public override Empty InitLargeArray(Empty input)
        {
            var arr = new int[1024 * 1024 * 1024]; // 

            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = int.MaxValue;
            }

            return new Empty();
        }

        public override Empty InitLargeStringDynamic(InitLargeStringDynamicInput input)
        {
            var str = new String('A', input.StringSizeValue);

            return new Empty();
        }

        public override Empty TestCallToNestedClass(Empty input)
        {
            State.CurrentTime.Value = NestedClass.UseDeniedMemberInNestedClass();

            return new Empty();
        }
        
        public override Empty TestCallToSeparateClass(Empty input)
        {
            State.CurrentTime.Value = SeparateClass.UseDeniedMemberInSeparateClass();
            
            return new Empty();
        }

        public override Empty TestInfiniteLoop(Empty input)
        {
            var list = new List<int>();
            while (true)
            {
                list.Add(int.MaxValue); // Just add any value to exhaust memory
            }
            return new Empty();
        }

        public override Empty TestInfiniteLoopInSeparateClass(Empty input)
        {
            SeparateClass.UseInfiniteLoopInSeparateClass();
            return new Empty();
        }

        private class NestedClass
        {
            public static DateTime UseDeniedMemberInNestedClass()
            {
                return DateTime.Now;
            }
        }
    }
    
    public class SeparateClass
    {
        public static DateTime UseDeniedMemberInSeparateClass()
        {
            return DateTime.Now;
        }

        public static void UseInfiniteLoopInSeparateClass()
        {
            var list = new List<int>();
            while (true)
            {
                list.Add(int.MaxValue);
            }
        }
    }
}
