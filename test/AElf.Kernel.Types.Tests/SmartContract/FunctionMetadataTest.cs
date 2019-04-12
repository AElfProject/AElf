using System;
using System.Collections.Generic;
using AElf.Kernel.SmartContract;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests.SmartContract
{
    public class FunctionMetadataTest
    {
        [Fact]
        public void IEquatable_FunctionMetadata_Equals_Other_True()
        {
            var callingSet = new HashSet<string>{"FuncOne","FuncTwo"};
            var fullResourceSet = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadWriteAccountSharing)
            };
            IEquatable<FunctionMetadata> functionMetadata = new FunctionMetadata(callingSet, fullResourceSet);
            
            var otherCallingSet = new HashSet<string>{"FuncOne","FuncTwo"};
            var otherFullResourceSet = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadWriteAccountSharing)
            };
            var otherFunctionMetadata = new FunctionMetadata(otherCallingSet, otherFullResourceSet);
            functionMetadata.Equals(otherFunctionMetadata).ShouldBeTrue();
        }
        
        [Fact]
        public void IEquatable_FunctionMetadata_Equals_Other_False()
        {
            var callingSet = new HashSet<string>{"FuncOne","FuncTwo"};
            var fullResourceSet = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadWriteAccountSharing)
            };
            IEquatable<FunctionMetadata> functionMetadata = new FunctionMetadata(callingSet, fullResourceSet);
            
            var otherCallingSet1 = new HashSet<string>{"FuncOne","FuncThree"};
            var otherFullResourceSet1 = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadWriteAccountSharing)
            };
            var otherFunctionMetadata1 = new FunctionMetadata(otherCallingSet1, otherFullResourceSet1);
            functionMetadata.Equals(otherFunctionMetadata1).ShouldBeFalse();

            var otherCallingSet2 = new HashSet<string> {"FuncOne", "FuncTwo"};
            var otherFullResourceSet2 = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadOnlyAccountSharing)
            };
            var otherFunctionMetadata2 = new FunctionMetadata(otherCallingSet2,otherFullResourceSet2);
            functionMetadata.Equals(otherFunctionMetadata2).ShouldBeFalse();
        }

        [Fact]
        public void IEquatable_FunctionMetadata_Equals_Null_False()
        {
            var callingSet = new HashSet<string>{"FuncOne","FuncTwo"};
            var fullResourceSet = new HashSet<Resource>
            {
                new Resource("ResourceOne",DataAccessMode.AccountSpecific),
                new Resource("ResourceTwo",DataAccessMode.ReadOnlyAccountSharing),
                new Resource("ResourceThree",DataAccessMode.ReadWriteAccountSharing)
            };
            IEquatable<FunctionMetadata> functionMetadata = new FunctionMetadata(callingSet, fullResourceSet);
            functionMetadata.Equals(null).ShouldBeFalse();
        }
    }
}