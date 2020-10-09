using System;
using System.Reflection;
using System.Text;
using System.Threading;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Runtime.CSharp.Tests.BadContract;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.whitelist
{
    public class WhitelistValidatorTests : CSharpCodeOpsTestBase
    {
        [Fact]
        public void TestWhitelistValidationTest()
        {
            var badContractModule = GetContractModule(typeof(BadContract));
            var validator = new WhitelistValidator(new WhitelistProvider());
            var validationResults = validator.Validate(badContractModule, CancellationToken.None);
            // assembly
            validationResults.ShouldContain(v =>
                v.Message == "Assembly AElf.Kernel.Core is not allowed.");

            validationResults.ShouldNotContain(v => v.Info != null &&
                                                    v.Info.ReferencingMethod.Contains("get_AllowedListField"));

            validationResults.ShouldContain(v => v.Info != null &&
                                                 v.Info.ReferencingMethod.Contains("get_DeniedListField"));
            validationResults.ShouldContain(v => v.Info != null &&
                                                 v.Info.Namespace == "System.Reflection" &&
                                                 v.Info.Type.Contains(nameof(Assembly)) &&
                                                 v.Info.ReferencingMethod.Contains("get_AssemblyField"));
            validationResults.ShouldNotContain(v => v.Info != null &&
                                                 v.Info.Namespace == "System.Reflection" &&
                                                 v.Info.Type.Contains(nameof(AssemblyCompanyAttribute)));

            validationResults.ShouldContain(v =>
                v.Info != null && v.Info.ReferencingMethod.Contains("get_RandomArray"));

            validationResults.ShouldContain(v =>
                v.Info != null && v.Info.Member != null && v.Info.Type.Contains(nameof(DateTime)) &&
                v.Info.Member.Contains("get_Today"));
            
            validationResults.ShouldNotContain(v =>
                v.Info != null && v.Info.Member != null && v.Info.Type.Contains(nameof(Encoding)) &&
                v.Info.Member.Contains("get_UTF8"));
            
            validationResults.ShouldContain(v =>
                v.Info != null && v.Info.Member != null && v.Info.Type.Contains(nameof(Environment)) &&
                v.Info.Member.Contains("get_CurrentManagedThreadId"));
            
            validationResults.ShouldContain(v => v.Info != null &&
                                                 v.Info.Namespace == "System.Reflection" &&
                                                 v.Info.Type.Contains(nameof(Assembly)));
            
            validationResults.ShouldContain(v =>
                v.Info != null && v.Info.Member == nameof(SecretSharingHelper.DecodeSecret) && v.Info.Type == (nameof(SecretSharingHelper)));
        }
    }
}