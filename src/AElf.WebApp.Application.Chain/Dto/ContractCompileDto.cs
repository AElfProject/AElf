using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto;

public class ContractCompileDto
{
    public string status { get; set; }
    public string msg { get; set; }
    public byte[] compileResult { get; set; }

    // 静态方法，返回失败的实例
    public static ContractCompileDto Fail(string message)
    {
        return new ContractCompileDto
        {
            status = "fail",
            msg = message,
            compileResult = null
        };
    }

    // 静态方法，返回成功的实例
    public static ContractCompileDto Succ(byte[] result)
    {
        return new ContractCompileDto
        {
            status = "success",
            msg = "Compilation succeeded",
            compileResult = result
        };
    }
}
