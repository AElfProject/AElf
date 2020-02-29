#tool nuget:?package=Codecov
#addin nuget:?package=Cake.Codecov
var target = Argument("target", "Default");
var rootPath     = "./";
var srcPath      = rootPath + "src/";
var contractPath = rootPath + "contract/";
var testPath     = rootPath + "test/";
var distPath     = rootPath + "aelf-node/";
var solution     = rootPath + "AElf.sln";
var srcProjects  = GetFiles(srcPath + "**/*.csproj");
var contractProjects  = GetFiles(contractPath + "**/*.csproj");

Task("Clean")
    .Description("clean up project cache")
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

Task("Restore")
    .Description("restore project dependencies")
    .Does(() =>
{
    var restoreSettings = new DotNetCoreRestoreSettings{
        ArgumentCustomization = args => {
            return args.Append("-v quiet");}
};
    DotNetCoreRestore(solution,restoreSettings);
});

Task("Build")
    .Description("Compilation project")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
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


Task("Test-with-Codecov")
    .Description("operation test_with_codecov")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testSetting = new DotNetCoreTestSettings{
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => {
            return args.Append("--logger trx")
                       .Append("/p:CollectCoverage=true")
                       .Append("/p:CoverletOutputFormat=json%2copencover")
                       .Append("/p:CoverletOutput=../results/coverage")
                       .Append("/p:MergeWith=../results/coverage.json")
                       .Append("/maxcpucount:1")
                       .Append("/p:Exclude=[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Kernel.Consensus.Scheduler.*]*%2c[AElf.Database]AElf.Database.RedisProtocol.*%2c[AElf.Test.Helpers]*%2c[*]*Exception%2c[*.Tests]*%2c[AElf.Contracts.GenesisUpdate]*%2c[AElf.WebApp.Application.Chain]*%2c[AElf.WebApp.Application.Net]*")
                       .Append("/p:ExcludeByFile=../../src/AElf.Runtime.CSharp.Core/Metadata/*.cs%2c../../src/AElf.Kernel.SmartContract/Metadata/*.cs%2c../../src/AElf.Database/RedisDatabase.cs%2c../../test/*.TestBase/*.cs");
        }                
    };
    var testProjects = GetFiles("./test/*.Tests/*.csproj");

    foreach(var testProject in testProjects)
    {
        DotNetCoreTest(testProject.FullPath, testSetting);
    }
});
Task("Run-Unit-Tests")
    .Description("operation test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testSetting = new DotNetCoreTestSettings{
        NoRestore = true,
        NoBuild = true
};
    var testProjects = GetFiles("./test/*.Tests/*.csproj");

    foreach(var testProject in testProjects)
    {
        DotNetCoreTest(testProject.FullPath, testSetting);
    }
});
Task("Upload-Coverage")
    .Does(() =>
{
    // Upload a coverage report.
    Codecov("./test/results/coverage.opencover.xml","$CODECOV_TOKEN");
});
Task("Default")
    .IsDependentOn("Run-Unit-Tests");

RunTarget(target);