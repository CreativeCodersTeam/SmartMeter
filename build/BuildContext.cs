using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using CreativeCoders.CakeBuild;
using CreativeCoders.CakeBuild.Tasks.Defaults;
using CreativeCoders.CakeBuild.Tasks.Templates.Settings;
using CreativeCoders.Core;
using CreativeCoders.Core.Collections;
using JetBrains.Annotations;

namespace Build;

[UsedImplicitly]
public class BuildContext(ICakeContext context)
    : CakeBuildContext(context), IDefaultTaskSettings, ICreateDistPackagesTaskSettings, ICreateGitHubReleaseTaskSettings
{
    public IList<DirectoryPath> DirectoriesToClean => this.CastAs<ICleanTaskSettings>()
        .GetDefaultDirectoriesToClean().AddRange(RootDir.Combine(".tests"));


    public string Copyright => $"{DateTime.Now.Year} CreativeCoders";

    public string PackageProjectUrl => "https://github.com/CreativeCodersTeam/SmartMeter";

    public string PackageLicenseExpression => PackageLicenseExpressions.ApacheLicense20;

    public string NuGetFeedUrl => this.GitHubActions().Environment.Workflow.Workflow == "release"
        ? "nuget.org"
        : "https://nuget.pkg.github.com/CreativeCodersTeam/index.json";

    public bool SkipPush => this.BuildSystem().IsPullRequest ||
                            this.BuildSystem().IsLocalBuild ||
                            this.GitHubActions().Environment.Runner.OS != "Linux";

    public DirectoryPath PublishOutputDir => ArtifactsDir.Combine("published");

    private const string CliPath = "source/CreativeCoders.SmartMeter.Cli";

    private const string CliProjectFile = "CreativeCoders.SmartMeter.Cli.csproj";

    public IEnumerable<PublishingItem> PublishingItems =>
    [
        new PublishingItem(
            RootDir
                .Combine(CliPath)
                .CombineWithFilePath(CliProjectFile),
            PublishOutputDir.Combine("cli"))
    ];

    private const string CliDistPackageName = "SmartMeter.Cli";

    public IEnumerable<DistPackage> DistPackages =>
    [
        new DistPackage(CliDistPackageName, PublishOutputDir.Combine("cli"))
    ];

    public string ReleaseName => $"v{Version.FullSemVer}";

    public string ReleaseVersion => $"v{Version.FullSemVer}";

    public string ReleaseBody => "SmartMeter Release";

    public bool IsPreRelease => !string.IsNullOrWhiteSpace(Version.PreReleaseTag);

    public IEnumerable<GitHubReleaseAsset> ReleaseAssets =>
    [
        new GitHubReleaseFileAsset(
            GetRequiredSettings<ICreateDistPackagesTaskSettings>().DistOutputPath
                .CombineWithFilePath(CliDistPackageName + ".tar.gz").FullPath, null)
    ];
}
