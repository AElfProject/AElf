using System;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.CSharp.Tests.BadContract;

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

    public override Empty TestCallToSeparateClass(Empty input)
    {
        State.CurrentTime.Value = SeparateClass.UseDeniedMemberInSeparateClass();
            
        return new Empty();
    }

    public override Empty TestInfiniteLoop(Int32Value input)
    {
        int i = 0;
        while (i++ < input.Value)
        {
        }
            
        ExecutionObserverProxy.SetObserver(null);
        return new Empty();
    }

    public override Empty TestInfiniteLoopInSeparateClass(Empty input)
    {
        SeparateClass.UseInfiniteLoopInSeparateClass();
        return new Empty();
    }

    public override Empty TestInfiniteRecursiveCall(Empty input)
    {
        InfiniteRecursiveCall();
        return new Empty();
    }
        
    private void InfiniteRecursiveCall(string text = "")
    {
        text += "TEST";
        InfiniteRecursiveCall(text);
    }

    public override Empty TestInfiniteRecursiveCallInSeparateClass(Empty input)
    {
        SeparateClass.UseInfiniteRecursiveCallInSeparateClass();
        return new Empty();
    }


    public override Int32Value TestGetHashCodeCall(Empty input)
    {
        return new Int32Value()
        {
            Value = new IMessageInheritedClass().GetHashCode()
        };
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
        int i = 0;
        for (; true;)
        {
            i++;
        }
    }

    public static void UseInfiniteRecursiveCallInSeparateClass(string text = "")
    {
        text += "TEST";
        UseInfiniteRecursiveCallInSeparateClass(text);
    }
}

public class IMessageInheritedClass : IMessage
{
    public int Test { get; set; }

    public override int GetHashCode()
    {
        Test = base.GetHashCode();
        return 0;
    }

    public void MergeFrom(CodedInputStream input)
    {
        throw new NotImplementedException();
    }

    public void WriteTo(CodedOutputStream output)
    {
        throw new NotImplementedException();
    }

    public int CalculateSize()
    {
        throw new NotImplementedException();
    }

    public MessageDescriptor Descriptor { get; }
}