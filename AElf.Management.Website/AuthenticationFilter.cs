using System.IO;
using System.Net;
using System.Net.Http;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AElf.Management.Website
{
    public class AuthenticationFilter: IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var signType = context.HttpContext.Request.Headers["auth-type"];
            
            if (signType == "apikey")
            {
                var sign = context.HttpContext.Request.Headers["sign"];
                var chainId = context.ActionArguments["chainId"];
                var method = context.HttpContext.Request.Method;
                var timestamp = context.HttpContext.Request.Headers["timestamp"];
                
                context.Result = new JsonResult(new ApiEmptyResult(401, "Unauthorized"));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }
    }
}