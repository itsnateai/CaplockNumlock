namespace CapsNumTray.Tests;

[TestClass]
public class UpdateDialogAllowlistTests
{
    // The allowlist replaced an earlier inline check that used substring-match —
    // `Contains(".githubusercontent.com/")` would have allowed
    // `https://evil.example/.githubusercontent.com/x.exe`. These tests pin the
    // Uri-host-based replacement.

    [TestMethod]
    public void NullOrEmpty_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(null));
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(""));
    }

    [TestMethod]
    public void NotAUri_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin("not a url"));
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin("/relative/path"));
    }

    [TestMethod]
    public void HttpScheme_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "http://github.com/itsnateai/CaplockNumlock/releases/download/v1/CapsNumTray.exe"));
    }

    [TestMethod]
    public void GitHubCom_CorrectRepo_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://github.com/itsnateai/CaplockNumlock/releases/download/v2.3.1/CapsNumTray.exe"));
    }

    [TestMethod]
    public void GitHubCom_WrongOwner_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://github.com/evil/CaplockNumlock/releases/download/v1/CapsNumTray.exe"));
    }

    [TestMethod]
    public void GitHubCom_WrongRepo_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://github.com/itsnateai/SomethingElse/releases/download/v1/file.exe"));
    }

    [TestMethod]
    public void ApiGitHub_CorrectRepoPath_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://api.github.com/repos/itsnateai/CaplockNumlock/releases/latest"));
    }

    [TestMethod]
    public void ApiGitHub_WrongRepoPath_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://api.github.com/repos/evil/CaplockNumlock/releases/latest"));
    }

    [TestMethod]
    public void ObjectsCdn_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://objects.githubusercontent.com/github-production-release-asset-anything"));
    }

    [TestMethod]
    public void ReleaseAssetsCdn_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://release-assets.githubusercontent.com/github-production-release-asset-anything"));
    }

    [TestMethod]
    public void HostConfusion_SubdomainOfAttacker_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://github.com.evil.example/itsnateai/CaplockNumlock/releases/download/v1/x.exe"));
    }

    [TestMethod]
    public void HostConfusion_AttackerWithGitHubInPath_Rejected()
    {
        // This is the case the prior `Contains(".githubusercontent.com/")` would
        // have let through. Uri-host parsing now blocks it.
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://evil.example/.githubusercontent.com/payload.exe"));
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://evil.example/github.com/itsnateai/CaplockNumlock/releases/download/v1/x.exe"));
    }

    [TestMethod]
    public void HostConfusion_LookalikeCdn_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://objects.githubusercontent.com.evil.example/anything"));
    }

    [TestMethod]
    public void DifferentGitHubProperty_Rejected()
    {
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://itsnateai.github.io/CaplockNumlock/index.html"));
        Assert.IsFalse(UpdateDialog.IsAllowedReleaseOrigin(
            "https://raw.githubusercontent.com/itsnateai/CaplockNumlock/main/README.md"));
    }

    [TestMethod]
    public void HostCaseInsensitive_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://GITHUB.COM/itsnateai/CaplockNumlock/releases/latest"));
    }

    [TestMethod]
    public void RepoPathCaseInsensitive_Allowed()
    {
        Assert.IsTrue(UpdateDialog.IsAllowedReleaseOrigin(
            "https://github.com/ITSNATEAI/CAPLOCKNUMLOCK/releases/latest"));
    }
}
