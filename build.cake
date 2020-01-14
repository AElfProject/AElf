#reference "NuGet.Packaging"

#load nuget.tool.cake

var target = Argument("target", "default");
var rootPath     = "./";
var srcPath      = rootPath + "src/";
var contractPath = rootPath + "contract/";
var testPath     = rootPath + "test/";
var distPath     = rootPath + "aelf-node/";

var solution     = rootPath + "AElf.sln";
var srcProjects  = GetFiles(srcPath + "**/*.csproj");
var contractProjects  = GetFiles(contractPath + "**/*.csproj");

var nugetTool = NuGetTool.FromCakeContext(Context);

Task("clean")
    .Description("清理项目缓存")
    .Does(() =>
{
    DeleteFiles(distPath + "*.nupkg");
    CleanDirectories(srcPath + "**/bin");
    CleanDirectories(srcPath + "**/obj");
    CleanDirectories(contractPath + "**/bin");
    CleanDirectories(contractPath + "**/obj");
    CleanDirectories(testPath + "**/bin");
    CleanDirectories(testPath + "**/obj");
});

Task("restore")
    .Description("还原项目依赖")
    .Does(() =>
{
    DotNetCoreRestore(solution);
});

Task("build")
    .Description("编译项目")
    .IsDependentOn("clean")
    .IsDependentOn("restore")
    .Does(() =>
{
    var buildSetting = new DotNetCoreBuildSettings{
        NoRestore = true,
        Configuration = "Debug",
        ArgumentCustomization = args => {
            return args.Append("/clp:ErrorsOnly")
                       .Append("/p:GeneratePackageOnBuild=false")
                       .Append("-v quiet");}
    };
     
    DotNetCoreBuild(solution, buildSetting);
});


Task("test")
    .Description("运行测试")
    .IsDependentOn("build")
    .Does(() =>
{
    var testSetting = new DotNetCoreTestSettings{
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => {
            return args.Append("/p:CollectCoverage=true")
                       .Append("/p:CoverletOutputFormat=opencover")
                       .Append("/p:CoverletOutput='./results/coverage'")
                       .Append("/p:MergeWith='./results/coverage.json'")
                       .Append("/p:Exclude='[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Kernel.Consensus.Scheduler.*]*%2c[AElf.Database]AElf.Database.RedisProtocol.*%2c[AElf.Contracts.Authorization]*%2c[AElf.Test.Helpers]*%2c[*]*Exception%2c[*.Tests]*%2c[AElf.Contracts.TestContract.BasicFunctionWithParallel]*%2c[AElf.Contracts.GenesisUpdate]*'")
                       .Append("/p:ExcludeByFile='../src/AElf.Runtime.CSharp.Core/Metadata/*.cs%2c../src/AElf.Kernel.SmartContract/Metadata/*.cs%2c../src/AElf.Database/RedisDatabase.cs%2c../test/*.TestBase/*.cs'");}                   
};
    var testProjects = GetFiles("./test/*.Tests/*.csproj");

    foreach(var testProject in testProjects)
    {
        DotNetCoreTest(testProject.FullPath, testSetting);
    }
});

Task("default")
    .Description("默认-运行测试(-target test)")
    .IsDependentOn("test");

RunTarget(target);
