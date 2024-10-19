using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AElf.ExceptionHandler;
using AElf.Kernel;
using Microsoft.Extensions.Logging;

namespace AElf.Runtime.CSharp;

public partial class UnitTestCSharpSmartContractRunner : CSharpSmartContractRunner
{
    public UnitTestCSharpSmartContractRunner(string sdkDir)
        : base(sdkDir)
    {
        Category = KernelConstants.CodeCoverageRunnerCategory;
    }

    protected override Assembly LoadAssembly(byte[] code, AssemblyLoadContext loadContext)
    {
        var assembly = base.LoadAssembly(code, loadContext);

        var assemblyByFullName = LoadAssemblyByFullName(assembly);

        if (assemblyByFullName != null && code.SequenceEqual(File.ReadAllBytes(assemblyByFullName.Location)))
        {
            assembly = assemblyByFullName;
        }

        return assembly;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(UnitTestCSharpSmartContractRunner),
        MethodName = nameof(HandleExceptionWhileLoadingAssemblyByFullName))]
    protected Assembly LoadAssemblyByFullName(Assembly assembly)
    {
        var assembly2 = Assembly.Load(assembly.FullName);
        return assembly2;
    }
}