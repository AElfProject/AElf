/// args
var target = Argument("target", "default");


/// build task

Task("build")
    .Does(() =>
{
    MSBuild("./AElf.sln", new MSBuildSettings{
        Verbosity = Verbosity.Minimal
    });
});


Task("build1")
    .Does(() =>
{
    MSBuild("./AElf.sln", new MSBuildSettings{
        Verbosity = Verbosity.Minimal
    });
});


Task("build2")
    .Does(() =>
{
    MSBuild("./AElf.All.sln", new MSBuildSettings{
        Verbosity = Verbosity.Minimal
    });
});

Task("build3")
    .Does(() =>
{
    MSBuild("./AElf.Management.sln", new MSBuildSettings{
        Verbosity = Verbosity.Minimal
    });
});




Task("default")
    .IsDependentOn("build");


/// run task
RunTarget(target);
