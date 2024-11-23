using System.Collections.Generic;
using CreativeCoders.Core.IO;
using CreativeCoders.NukeBuild.Components.Parameters;
using CreativeCoders.NukeBuild.Components.Targets;
using CreativeCoders.NukeBuild.Components.Targets.Settings;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;

[GitHubActions("integration", GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["feature/**"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = ["clean", "restore", "compile", "publish"],
    EnableGitHubToken = true,
    PublishArtifacts = true,
    FetchDepth = 0
)]
[GitHubActions("main", GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["main"],
    InvokedTargets = ["clean", "restore", "compile", "publish"],
    EnableGitHubToken = true,
    PublishArtifacts = true,
    FetchDepth = 0
)]
[GitHubActions(ReleaseWorkflow, GitHubActionsImage.UbuntuLatest,
    OnPushTags = ["v**"],
    InvokedTargets = ["clean", "restore", "compile", "publish", "CreateDistPackages", "CreateGithubRelease"],
    EnableGitHubToken = true,
    PublishArtifacts = true,
    FetchDepth = 0
)]
class Build : NukeBuild,
    IGitRepositoryParameter,
    IConfigurationParameter,
    IGitVersionParameter,
    ISourceDirectoryParameter,
    IArtifactsSettings,
    ICleanTarget, IBuildTarget, IRestoreTarget, IPublishTarget, ICreateDistPackagesTarget, ICreateGithubReleaseTarget
{
    const string ReleaseWorkflow = "release";
    
    public static int Main () => Execute<Build>(x => ((IBuildTarget)x).Build);

    public Build()
    {
        FileSys.Directory.CreateDirectory(DistOutputPath);
    }
    
    [Parameter(Name = "GITHUB_TOKEN")] string GitHubToken;

    IEnumerable<PublishingItem> IPublishSettings.PublishingItems =>
    [
        new PublishingItem(
            GetSourceDir() / "CreativeCoders.SmartMeter.Server.Linux" / "CreativeCoders.SmartMeter.Server.Linux.csproj",
            GetDistDir() / "smartmetersrv")
    ];

    string GetVersion() => ((IGitVersionParameter) this).GitVersion?.NuGetVersionV2 ?? "0.1-unknown";

    AbsolutePath GetSourceDir() => ((ISourceDirectoryParameter) this).SourceDirectory;

    AbsolutePath GetDistDir() => ((IArtifactsSettings) this).ArtifactsDirectory / "dist";

    public IEnumerable<DistPackage> DistPackages =>
    [
        new DistPackage($"smartmeter-{GetVersion()}", GetDistDir() / "smartmeter") { Format = DistPackageFormat.TarGz }
    ];

    public AbsolutePath DistOutputPath => GetDistDir() / "packages";

    public string ReleaseName => $"Release {GetVersion()}";
    
    public string ReleaseBody => $"Release {GetVersion()}";

    public string ReleaseVersion => GetVersion();

    public IEnumerable<GithubReleaseAsset> ReleaseAssets =>
    [
        new GithubReleaseAsset(DistOutputPath / $"smartmetersrv-{GetVersion()}.tar.gz")
    ];
}
