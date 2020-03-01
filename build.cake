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
                       .Append("--collect:\"XPlat Code Coverage\"");
        }                
    };
    var codecovToken = "$CODECOV_TOKEN";
    var actions = new List<Action>();
    var testProjects = GetFiles("./test/*.Tests/*.csproj");

    foreach(var testProject in testProjects)
    {
        var action=new Action(()=>{
            DotNetCoreTest(testProject.FullPath, testSetting);

            if(codecovToken!=""){
                var dir=testProject.GetDirectory().FullPath;
                var reports = GetFiles(dir + "/TestResults/*/coverage.cobertura.xml");

                foreach(var report in reports)
                {
                    Codecov(report.FullPath,"$CODECOV_TOKEN");
                }
            }
        });
        actions.Add(action);
    }


    var options = new ParallelOptions {
        MaxDegreeOfParallelism = 2,
        //CancellationToken = cancellationToken
    };

    Parallel.Invoke(options, actions.ToArray());
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
    var reports = GetFiles("./test/*.Tests/TestResults/*/coverage.cobertura.xml");

    foreach(var report in reports)
    {
        Codecov(report.FullPath,"$CODECOV_TOKEN");
    }
    // Upload a coverage report.
});
Task("Default")
    .IsDependentOn("Run-Unit-Tests");

RunTarget(target);