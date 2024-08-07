using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.WebApp.Application.Chain;

public interface IContractCompileService
{
    Task<ContractCompileDto> UploadAndCompileForwardAsync(IFormFile file, string fileType);
    byte[] CompileAsync(IFormFile file, string fileType);
}

public class ContractCompileService
    : AElfAppService, IContractCompileService
{
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly CompileContractOptions _compileContractOptions ;
    private readonly ISolangEndpointProvider _solangEndpointProvider;
    private readonly IBlockchainService _blockchainService;

    public ContractCompileService(ISolangEndpointProvider solangEndpointProvider,
        IOptionsMonitor<CompileContractOptions> compileContractOptions,
        IBlockchainService blockchainService)
    {
        _solangEndpointProvider = solangEndpointProvider;
        _compileContractOptions = compileContractOptions.CurrentValue;
        _blockchainService = blockchainService;
    }


    public async Task<ContractCompileDto> UploadAndCompileForwardAsync(IFormFile file, string fileType)
    {
        if (file == null || fileType == null)
        {
            return ContractCompileDto.Fail("file or fileType is null");
        }

        var tempFilePath = Path.GetTempFileName();
        await SaveFileAsync(file, tempFilePath);

        try
        {
            var response = await SendCompileRequestAsync(file, fileType, tempFilePath);
            return await HandleResponseAsync(response);
        }
        catch (Exception e)
        {
            Logger.LogError("UploadAndCompileForwardAsync error: " + e.Message);
            return ContractCompileDto.Fail(e.Message);
        }
        finally
        {
            ClearTempFiles(new[] { tempFilePath });
        }
    }

    public byte[] CompileAsync(IFormFile file, string fileType)
    {
        var path = SaveContractFileTemp(file);
        return CompileContractBySoLang(path, fileType) ? GetCompiledFile(path, file) : null;
    }

    private async Task SaveFileAsync(IFormFile file, string tempFilePath)
    {
        await using var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(stream);
    }

    private async Task<HttpResponseMessage> SendCompileRequestAsync(IFormFile file, string fileType, string tempFilePath)
    {
        using var multipartFormDataContent = new MultipartFormDataContent();
        var fileContent = new StreamContent(new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
        };

        multipartFormDataContent.Add(fileContent, "file", file.FileName);
        multipartFormDataContent.Add(new StringContent(fileType), "fileType");

        // SetRequestHeaders();
        
        var chain = await _blockchainService.GetChainAsync();
        var solangEndpointAsync = await _solangEndpointProvider.GetSolangEndpointAsync(new BlockIndex
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        });
        return await HttpClient.PostAsync(_compileContractOptions.SolangServerEndpoint, multipartFormDataContent);
    }

    private void SetRequestHeaders()
    {
        // HttpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7");
        // HttpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        // HttpClient.DefaultRequestHeaders.Accept.ParseAdd("text/plain; v=1.0");
        // HttpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"99\", \"Google Chrome\";v=\"127\", \"Chromium\";v=\"127\"");
        // HttpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        // HttpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"macOS\"");
    }

    private async Task<ContractCompileDto> HandleResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("HTTP POST request failed with status code: {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"HTTP POST request failed with status code: {response.StatusCode}");
        }

        Logger.LogInformation("HTTP POST request succeeded.");
        var readAsByteArrayAsync = await response.Content.ReadAsByteArrayAsync();
        return ContractCompileDto.Succ(readAsByteArrayAsync);
    }

    private string SaveContractFileTemp(IFormFile file)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);
        using var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
        file.CopyTo(stream);
        return tempFilePath;
    }

    private bool CompileContractBySoLang(string filePath, string fileType)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "solang",
                Arguments = $"compile --target polkadot --output {Path.GetTempPath()} {filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Logger.LogError("Compilation failed with error: {Error}", error);
                return false;
            }

            Logger.LogInformation("Compilation succeeded. Output: {Output}", output);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred while compiling contract");
            return false;
        }
    }

    private byte[] GetCompiledFile(string originalFilePath, IFormFile file)
    {
        var compiledWasmPath = Path.Combine(Path.GetTempPath(), file.FileName.Replace(".sol", ".wasm"));
        try
        {
            if (File.Exists(compiledWasmPath))
            {
                return File.ReadAllBytes(compiledWasmPath);
            }
            else
            {
                Logger.LogError("Compiled file not found at {CompiledFilePath}", compiledWasmPath);
            }
        }
        finally
        {
            ClearTempFiles(new[] { originalFilePath, compiledWasmPath });
        }

        return null;
    }

    private void ClearTempFiles(string[] paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
