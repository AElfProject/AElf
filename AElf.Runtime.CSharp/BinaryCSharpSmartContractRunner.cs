using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;

namespace AElf.Runtime.CSharp
{
//    [Serializable]
    public class InvalidCodeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public InvalidCodeException()
        {
        }

        public InvalidCodeException(string message) : base(message)
        {
        }

        public InvalidCodeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidCodeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    
    public class BinaryCSharpSmartContractRunner : ISmartContractRunner
    {

        private SmartContractZero _smartContractZero;
        private CSharpAssemblyLoadContext _loadContext;

        public BinaryCSharpSmartContractRunner(SmartContractZero smartContractZero, CSharpAssemblyLoadContext loadContext)
        {
            _smartContractZero = smartContractZero;
            _loadContext = loadContext;
        }
        
        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment, 
            IAccountDataProvider adp)
        {
            
//            var smartContractZero = typeof(Class1);
//            var typeName = smartContractZero.AssemblyQualifiedName;
//            var type = System.Type.GetType(typeName);

            var code = reg.ContractBytes.ToByteArray();
            
            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = _loadContext.LoadFromStream(stream);
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }
            
            var type = assembly.GetTypes().FirstOrDefault(x => x.BaseType.Name.EndsWith("CSharpSmartContract"));
            if(type == null)
            {
                throw new InvalidCodeException("No SmartContract type is defined in the code.");
            }
            
            // construct instance
            var constructorParams = Parameters.Parser.ParseFrom(deployment.ConstructParams).Params;
            var parameterObjs = constructorParams.Select(p => p.Value()).ToArray();
            var paramTypes = parameterObjs.Select(p => p.GetType()).ToArray();
            var constructorInfo = type.GetConstructor(paramTypes);

//            var instance = Activator.CreateInstance(type);
            
            var instance = (IContextedSmartContract) constructorInfo.Invoke(parameterObjs);
            instance?.SetDataProvider(adp.GetDataProvider());

            return await Task.FromResult(instance);
        }
    }
}