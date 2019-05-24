using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class ArrayValidator : IValidator<MethodDefinition>
    {
        private const long AllowedTotalSize = 40 * 1024; // Byte per array when limiting by total array size

        private static readonly ArrayLimitLookup AllowedTypes = new ArrayLimitLookup()
            .LimitByTotalSize(typeof(Int16), sizeof(Int16))
            .LimitByTotalSize(typeof(Int32), sizeof(Int32))
            .LimitByTotalSize(typeof(Int64), sizeof(Int64))
            .LimitByTotalSize(typeof(UInt16), sizeof(UInt16))
            .LimitByTotalSize(typeof(UInt32), sizeof(UInt32))
            .LimitByTotalSize(typeof(UInt64), sizeof(UInt64))
            .LimitByTotalSize(typeof(decimal), sizeof(decimal))
            .LimitByTotalSize(typeof(char), sizeof(char))
            .LimitByTotalSize(typeof(String), 128) // Need to limit the size of strings by disallowing String.Concat
            
            // It isn't possible to estimate runtime sizes for below, so limit by count
            .LimitByCount(typeof(Type), 5)
            .LimitByCount(typeof(Object), 5) // Support object in Linq queries
            .LimitByCount("Google.Protobuf.Reflection.FileDescriptor", 10)
            .LimitByCount("Google.Protobuf.Reflection.GeneratedClrTypeInfo", 100);

        // When array dimension is 8 or lower, below OpCodes are used, get size from lookup
        private static readonly Dictionary<OpCode, int> PushIntLookup = new Dictionary<OpCode, int>()
        {
            {OpCodes.Ldc_I4_0, 0},
            {OpCodes.Ldc_I4_1, 1},
            {OpCodes.Ldc_I4_2, 2},
            {OpCodes.Ldc_I4_3, 3},
            {OpCodes.Ldc_I4_4, 4},
            {OpCodes.Ldc_I4_5, 5},
            {OpCodes.Ldc_I4_6, 6},
            {OpCodes.Ldc_I4_7, 7},
            {OpCodes.Ldc_I4_8, 8},
        };

        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();
            
            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode != OpCodes.Newarr)
                    continue;

                var typ = ((TypeReference) instruction.Operand).FullName;

                ArrayValidationResult error = null;
                if (AllowedTypes.TryGetLimit(typ, out var limit))
                {
                    if (TryGetArraySize(instruction, out var arrayDimension))
                    {
                        if (limit.By == LimitBy.Count)
                        {
                            if (arrayDimension > limit.Count)
                                error = new ArrayValidationResult($"Array size can not be larger than {limit.Count} elements. ({arrayDimension} x {typ})");
                        }
                        else
                        {
                            try
                            {
                                var totalSize = arrayDimension.Mul(limit.ElementSize);

                                if (totalSize > AllowedTotalSize)
                                    error = new ArrayValidationResult($"Array size can not be larger than {AllowedTotalSize} bytes. ({arrayDimension} x {typ})");
                            }
                            catch (OverflowException)
                            {
                                error = new ArrayValidationResult($"Array size is too large that causes overflow when estimating memory usage.");
                            }
                        }
                    }
                    else
                    {
                        error = new ArrayValidationResult($"Array size could not be identified for {typ}." + GetIlCodesPartial(instruction));
                    }
                }
                else
                {
                    error = new ArrayValidationResult($"Array of {typ} type is not allowed.");
                }
                
                if (error != null)
                    errors.Add(error.WithInfo(method.Name, method.GetType().Namespace, method.GetType().Name, null));
            }

            return errors;
        }

        private bool TryGetArraySize(Instruction instruction, out int size)
        {
            // Look for size declaration before newarr OpCode
            var previous = instruction.Previous;
            
            // Array size should be hardcoded, not from another method or variable
            if (previous.OpCode == OpCodes.Ldc_I4 || previous.OpCode == OpCodes.Ldc_I4_S)
            {
                size = Convert.ToInt32(previous.Operand);
                return true;
            }

            // If array size is set like ldc.i4.1 etc, get the size from look up
            if (PushIntLookup.TryGetValue(previous.OpCode, out size))
            {
                return true;
            }

            size = -1;
            return false;
        }

        private string GetIlCodesNext(Instruction instruction, int depth)
        {
            var code = "";
            while (depth-- > 0)
            {
                instruction = instruction.Next;
                code += instruction + "\n";
            }
            return code;
        }

        private string GetIlCodesPrevious(Instruction instruction, int depth)
        {
            var code = "";
            while (depth-- > 0)
            {
                instruction = instruction.Previous;
                code += "\n" + instruction + code;
            }
            return code;
        }

        private string GetIlCodesPartial(Instruction instruction, int depth = 5)
        {
            return GetIlCodesPrevious(instruction, depth) + "\n" + 
                   instruction + "\n" + 
                   GetIlCodesNext(instruction, depth);
        }

        private class ArrayLimitLookup
        {
            private Dictionary<string, ArrayLimit> _lookup = new Dictionary<string, ArrayLimit>();

            public ArrayLimitLookup LimitByCount(string type, int count)
            {
                _lookup.Add(type, new ArrayLimit(LimitBy.Count, count));
                return this;
            }
            
            public ArrayLimitLookup LimitByCount(Type type, int count)
            {
                return LimitByCount(type.FullName, count);
            }

            public ArrayLimitLookup LimitByTotalSize(Type type, int size)
            {
                _lookup.Add(type.FullName, new ArrayLimit(LimitBy.Size, size));
                return this;
            }

            public bool TryGetLimit(string type, out ArrayLimit limit)
            {
                return _lookup.TryGetValue(type, out limit);
            }
        }

        private class ArrayLimit
        {
            public LimitBy By { get; }
            
            public int ElementSize { get; }

            public int Count { get; }

            public ArrayLimit(LimitBy by, int num)
            {
                By = by;
                
                if (by == LimitBy.Count)
                {
                    Count = num;
                }
                else
                {
                    ElementSize = num;
                }
            }
        }
        
        private enum LimitBy
        {
            Count,
            Size
        }
    }

    public class ArrayValidationResult : ValidationResult
    {
        public ArrayValidationResult(string message) : base(message)
        {
        }
    }
}