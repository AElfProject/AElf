#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
/// argsddfff
var target = Argument("target", "default");
var configuration = Argument("configuration", "Release");
var solution = "./AElf.sln";

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore("./AElf.sln");
    NuGetRestore("./AElf.Console.sln");
    NuGetRestore("./AElf.Management.sln");
});



Task("AElf.sln")
    .Does(() =>
{
      // Use MSBuild
      MSBuild("./AElf.sln", settings =>
      settings.SetConfiguration(configuration));
});



Task("AElf.Console.sln")
    .Does(() =>
{
      // Use MSBuild
      MSBuild("./AElf.Console.sln", settings =>
      settings.SetConfiguration(configuration));
});



Task("AElf.Management.sln")
    .Does(() =>
{
      // Use MSBuild
      MSBuild("./AElf.Management.sln", settings =>
      settings.SetConfiguration(configuration));
});



Task("Run-Unit-Tests2")
    .Does(() =>
{
    NUnit3("./test/*/bin/Debug/netcoreapp2.2/*.dll", new NUnit3Settings {
        NoResults = true
        });
});


Task("Run-Unit-Tests")
    .Does(() =>
{
    MSTest("./test/*/bin/Debug/netcoreapp2.2/*.dll");
});

Task("Test")
    .Does(() =>
    {
        var projects = GetFiles("./test/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true
                });
        }
 });


Task("default")
   .IsDependentOn("Restore-NuGet-Packages")
   .IsDependentOn("AElf.sln")
   .IsDependentOn("AElf.Console.sln")
   .IsDependentOn("AElf.Management.sln")
   .IsDependentOn("Run-Unit-Tests");
    
    
RunTarget(target);
