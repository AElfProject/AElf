using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Shared.Tests
{
    public class ContextVariableDictionaryTests
    {
        [Fact]
        public void GetStringArray_Test()
        {
           var  dictionary= new Dictionary<string, string>()
                {
                    {"key1","Value1"},
                    {"key2","Value2"},
                    {"key3","Value3"},
                    {"key4","Value4"},
                }
                ;
           var contextVariableDictionary = new ContextVariableDictionary(dictionary);
          var result=contextVariableDictionary.GetStringArray("key1");
          //not in property yet
          foreach (var r in result)
          {
              r.ShouldBe("Value1");
          }
          // already in property
          var result1=contextVariableDictionary.GetStringArray("key1");
          foreach (var r in result1)
          {
              r.ShouldBe("Value1");
          }
          var result2 = contextVariableDictionary.GetStringArray("key5");
          result2.ShouldBeEmpty();
        }

        [Fact]
        public void ContractCallException_Test()
        {
            Should.Throw<ContractCallException>(() => createException<ContractCallException>());
            Should.Throw<ContractCallException>(() => createExceptionWithMessage("ContractCallException"));
            Should.Throw<ContractCallException>(() => createExceptionWithMessageAndError("ContractCallException"));

            Should.Throw<NoPermissionException>(() => createException<NoPermissionException>());
            Should.Throw<NoPermissionException>(() => createExceptionWithMessage("NoPermissionException"));
            Should.Throw<NoPermissionException>(() => createExceptionWithMessageAndError("NoPermissionException"));
            
            Should.Throw<StateOverSizeException>(() => createException<StateOverSizeException>());
            Should.Throw<StateOverSizeException>(() => createExceptionWithMessage("StateOverSizeException"));
            Should.Throw<StateOverSizeException>(() => createExceptionWithMessageAndError("StateOverSizeException"));

        }
        private void createException<T>() where T:SmartContractBridgeException,new()
        {
            T t = System.Activator.CreateInstance<T>();
            throw t;
        }
        private void createExceptionWithMessage(string type)
        {
            switch (type)
            {
                case "ContractCallException":throw new ContractCallException(type);
                case "NoPermissionException":throw new NoPermissionException(type);
                case "StateOverSizeException":throw new StateOverSizeException(type);
            }
        }
        private void createExceptionWithMessageAndError(string type)
        {
           var error= new Exception();
           switch (type)
            {
                case "ContractCallException":throw new ContractCallException(type,error);
                case "NoPermissionException":throw new NoPermissionException(type,error);
                case "StateOverSizeException":throw new StateOverSizeException(type,error);
            }
        }
    }
}