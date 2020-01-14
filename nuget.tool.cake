using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.List;
using Cake.Core;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;

public class NuGetTool
{
    public ICakeContext CakeContext { get; }

    public string RepositoryApiUrl { get; }

    public string RepositoryApiKey { get; }

    private DotNetCoreNuGetPushSettings PushSettings => new DotNetCoreNuGetPushSettings
    {
        Source = this.RepositoryApiUrl,
        ApiKey = this.RepositoryApiKey,
        IgnoreSymbols = false
    };

    private NuGetListSettings ListSettings => new NuGetListSettings
    {
        AllVersions = true,
        Source = new string[] { this.RepositoryApiUrl }
    };

    private DotNetCorePackSettings BuildPackSettings(string packOutputDirectory) => new DotNetCorePackSettings
    {
        Configuration = "Release",
        OutputDirectory = packOutputDirectory,
        IncludeSource = true,
        IncludeSymbols = true,
        NoBuild = false
    };

    private NuGetTool(ICakeContext cakeContext)
    {
        CakeContext = cakeContext;
        RepositoryApiUrl = cakeContext.Environment.GetEnvironmentVariable("NUGET_REPOSITORY_API_URL");
        RepositoryApiKey = cakeContext.Environment.GetEnvironmentVariable("NUGET_REPOSITORY_API_KEY");
    }

    public static NuGetTool FromCakeContext(ICakeContext cakeContext)
    {
        return new NuGetTool(cakeContext);
    }

    public void Pack(List<string> projectFilePaths, string packOutputDirectory)
    {
        projectFilePaths.ForEach(_ => CakeContext.DotNetCorePack(_, BuildPackSettings(packOutputDirectory)));
    }

    public void Push(List<string> packageFilePaths)
    {
        var pushResult = new NuGetPushResult(CakeContext, PushSettings, packageFilePaths);
        foreach (var packageFilePath in packageFilePaths)
        {
            var packageMetadata = GetPackageMetadata(packageFilePath);
            pushResult.PackageMetadatas.Add(packageMetadata);
            try
            {
                if (CheckPackageIsPushed(packageMetadata))
                {
                    pushResult.ExistsPackageMetadatas.Add(packageMetadata);
                    continue;
                }
                Push(packageFilePath);
                pushResult.PushSucceedPackageMetadatas.Add(packageMetadata);
            }
            catch (Exception e)
            {
                CakeContext.Error(e);
                pushResult.PushFailedPackageMetadatas.Add(packageMetadata);
            }
        }

        pushResult.Print();
    }

    private IPackageMetadata GetPackageMetadata(string filePath)
    {
        using (var fileStream = new PackageArchiveReader(filePath).GetNuspec())
        {
            return Manifest.ReadFrom(fileStream, false).Metadata;
        }
    }

    private bool CheckPackageIsPushed(IPackageMetadata packageMetadata)
    {
        var packages = CakeContext.NuGetList(packageMetadata.Id, ListSettings);
        return packages.Any(_ => _.Name == packageMetadata.Id && _.Version == packageMetadata.Version.ToString());
    }

    private void Push(string packageFilePath)
    {
        CakeContext.DotNetCoreNuGetPush(packageFilePath, PushSettings);
    }


    private class NuGetPushResult
    {
        private ICakeContext CakeContext { get; }

        private DotNetCoreNuGetPushSettings PushSettings { get; }

        private List<string> PackageFilePaths { get; }

        public List<IPackageMetadata> PackageMetadatas { get; } = new List<IPackageMetadata>();

        public List<IPackageMetadata> PushSucceedPackageMetadatas { get; } = new List<IPackageMetadata>();

        public List<IPackageMetadata> ExistsPackageMetadatas { get; } = new List<IPackageMetadata>();

        public List<IPackageMetadata> PushFailedPackageMetadatas { get; } = new List<IPackageMetadata>();

        public NuGetPushResult(ICakeContext cakeContext, DotNetCoreNuGetPushSettings pushSettings, List<string> packageFilePaths)
        {
            CakeContext = cakeContext;
            PushSettings = pushSettings;
            PackageFilePaths = packageFilePaths;
        }


        public void Print()
        {
            CakeContext.Information("\n发布{0}个包到 {1} 结果：成功{2},已存在{3},失败{4}。",
                PackageMetadatas.Count,
                PushSettings.Source,
                PushSucceedPackageMetadatas.Count,
                ExistsPackageMetadatas.Count,
                PushFailedPackageMetadatas.Count
            );

            foreach (var packageMetadata in PushSucceedPackageMetadatas)
            {
                CakeContext.Information(Format(packageMetadata, "发布成功"));
            }

            foreach (var packageMetadata in ExistsPackageMetadatas)
            {
                CakeContext.Warning(Format(packageMetadata, "已经存在，忽略发布"));
            }

            foreach (var packageMetadata in PushFailedPackageMetadatas)
            {
                CakeContext.Error(Format(packageMetadata, "发布失败"));
            }
        }

        private string Format(IPackageMetadata packageMetadata, string message)
        {
            return $"{packageMetadata.Id,-36} {packageMetadata.Version,-10} {message}";
        }
    }
}
