using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Authentication;

namespace AElf.WebApp.Web;

public partial class BasicAuthenticationHandler
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingUserNameAndPassword()
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"))
        };
    }
}