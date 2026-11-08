using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace BetterModMenu.Tests;

[TestClass]
public class ProjectFileTests
{
    [TestMethod]
    public void RunSteamWorkshopUpload_StagesContentAndRunsUploaderFromUploaderRoot()
    {
        using TempDirectory temp = new();
        string uploaderRoot = Path.Combine(temp.Path, "ModUploader-win-x64");
        string workshopDir = Path.Combine(uploaderRoot, "workshop", "BetterModMenu");
        string fakeDll = Path.Combine(temp.Path, "build", "BetterModMenu.dll");
        string capturePath = Path.Combine(temp.Path, "capture.txt");

        Directory.CreateDirectory(uploaderRoot);
        Directory.CreateDirectory(workshopDir);
        Directory.CreateDirectory(Path.GetDirectoryName(fakeDll)!);
        File.WriteAllText(Path.Combine(workshopDir, "workshop.json"), "{}");
        File.WriteAllText(fakeDll, "fake dll");
        BuildFakeUploader(temp.Path, uploaderRoot);

        DotnetResult result = RunDotnet(
            "msbuild",
            ProjectPath,
            "-t:RunSteamWorkshopUpload",
            $"-p:ModUploaderRoot={uploaderRoot}",
            $"-p:ModUploaderWorkshopDir={workshopDir}",
            $"-p:TargetPath={fakeDll}",
            "-p:TargetFileName=BetterModMenu.dll",
            capturePath);

        Assert.AreEqual(0, result.ExitCode, result.Output);
        Assert.IsTrue(File.Exists(Path.Combine(workshopDir, "content", "BetterModMenu.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(workshopDir, "content", "BetterModMenu.json")));

        string[] captured = File.ReadAllLines(capturePath);
        Assert.AreEqual(Path.GetFullPath(uploaderRoot), Path.GetFullPath(captured[0]));
        CollectionAssert.AreEqual(
            new[] { "upload", "-w", GetUploadPath(workshopDir) },
            captured.Skip(1).ToArray());
    }

    [TestMethod]
    public void RunSteamWorkshopUpload_FailsBeforeUploaderWhenUploaderIsMissing()
    {
        using TempDirectory temp = new();
        string uploaderRoot = Path.Combine(temp.Path, "ModUploader-win-x64");
        string workshopDir = Path.Combine(uploaderRoot, "workshop", "BetterModMenu");
        string fakeDll = Path.Combine(temp.Path, "build", "BetterModMenu.dll");
        string capturePath = Path.Combine(temp.Path, "capture.txt");

        Directory.CreateDirectory(workshopDir);
        Directory.CreateDirectory(Path.GetDirectoryName(fakeDll)!);
        File.WriteAllText(Path.Combine(workshopDir, "workshop.json"), "{}");
        File.WriteAllText(fakeDll, "fake dll");

        DotnetResult result = RunDotnet(
            "msbuild",
            ProjectPath,
            "-t:RunSteamWorkshopUpload",
            $"-p:ModUploaderRoot={uploaderRoot}",
            $"-p:ModUploaderWorkshopDir={workshopDir}",
            $"-p:TargetPath={fakeDll}",
            "-p:TargetFileName=BetterModMenu.dll",
            capturePath);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, "ModUploader.exe not found");
        Assert.IsFalse(File.Exists(capturePath));
    }

    [TestMethod]
    public void RunSteamWorkshopUpload_FailsBeforeUploaderWhenWorkshopIsMissing()
    {
        using TempDirectory temp = new();
        string uploaderRoot = Path.Combine(temp.Path, "ModUploader-win-x64");
        string workshopDir = Path.Combine(uploaderRoot, "workshop", "BetterModMenu");
        string fakeDll = Path.Combine(temp.Path, "build", "BetterModMenu.dll");
        string capturePath = Path.Combine(temp.Path, "capture.txt");

        Directory.CreateDirectory(workshopDir);
        Directory.CreateDirectory(Path.GetDirectoryName(fakeDll)!);
        File.WriteAllText(fakeDll, "fake dll");
        File.WriteAllText(Path.Combine(uploaderRoot, "ModUploader.exe"), "placeholder");

        DotnetResult result = RunDotnet(
            "msbuild",
            ProjectPath,
            "-t:RunSteamWorkshopUpload",
            $"-p:ModUploaderRoot={uploaderRoot}",
            $"-p:ModUploaderWorkshopDir={workshopDir}",
            $"-p:TargetPath={fakeDll}",
            "-p:TargetFileName=BetterModMenu.dll",
            capturePath);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, "Steam Workshop workspace not found");
        Assert.IsFalse(File.Exists(capturePath));
    }

    [TestMethod]
    public void RunSteamWorkshopUpload_FailsBeforeUploaderWhenContentHasNestedUnexpectedFiles()
    {
        using TempDirectory temp = new();
        string uploaderRoot = Path.Combine(temp.Path, "ModUploader-win-x64");
        string workshopDir = Path.Combine(uploaderRoot, "workshop", "BetterModMenu");
        string staleDir = Path.Combine(workshopDir, "content", "old");
        string fakeDll = Path.Combine(temp.Path, "build", "BetterModMenu.dll");
        string capturePath = Path.Combine(temp.Path, "capture.txt");

        Directory.CreateDirectory(uploaderRoot);
        Directory.CreateDirectory(staleDir);
        Directory.CreateDirectory(Path.GetDirectoryName(fakeDll)!);
        File.WriteAllText(Path.Combine(workshopDir, "workshop.json"), "{}");
        File.WriteAllText(Path.Combine(staleDir, "stale.txt"), "stale");
        File.WriteAllText(fakeDll, "fake dll");
        File.WriteAllText(Path.Combine(uploaderRoot, "ModUploader.exe"), "placeholder");

        DotnetResult result = RunDotnet(
            "msbuild",
            ProjectPath,
            "-t:RunSteamWorkshopUpload",
            $"-p:ModUploaderRoot={uploaderRoot}",
            $"-p:ModUploaderWorkshopDir={workshopDir}",
            $"-p:TargetPath={fakeDll}",
            "-p:TargetFileName=BetterModMenu.dll",
            capturePath);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, "ModUploader content folder contains extra files");
        Assert.IsFalse(File.Exists(capturePath));
    }

    [TestMethod]
    public void UploadSteamWorkshop_IsExplicitAndBuildBacked()
    {
        var targets = XDocument.Load(ProjectPath).Descendants("Target").ToArray();
        var upload = targets.Single(target => (string?)target.Attribute("Name") == "UploadSteamWorkshop");
        string[] dependencies = ((string?)upload.Attribute("DependsOnTargets") ?? "").Split(';', StringSplitOptions.TrimEntries);
        CollectionAssert.Contains(dependencies, "Build");
        CollectionAssert.Contains(dependencies, "RunSteamWorkshopUpload");
        foreach (var target in targets.Where(target => (string?)target.Attribute("Name") is "UploadSteamWorkshop" or "RunSteamWorkshopUpload"))
        {
            Assert.IsNull(target.Attribute("AfterTargets"));
            Assert.IsNull(target.Attribute("BeforeTargets"));
        }
    }

    [TestMethod]
    public void LocalizationCatalogs_AreEvaluatedAsEmbeddedResources()
    {
        using var evaluated = QueryProject("-getItem:EmbeddedResource");
        var resources = evaluated.RootElement.GetProperty("Items").GetProperty("EmbeddedResource")
            .EnumerateArray().Select(item => Path.GetFullPath(item.GetProperty("FullPath").GetString()!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string catalog in Directory.EnumerateFiles(Path.Combine(RepoRoot, "Localization"), "*.json"))
            Assert.IsTrue(resources.Contains(Path.GetFullPath(catalog)), $"Catalog not embedded: {catalog}");
    }

    [TestMethod]
    public void CloudPackagingSettings_AreEnabledOnlyWhenOptedIn()
    {
        using var defaults = QueryProject("-getProperty:IncludeCloudFeatures,CloudPackageSuffix,DefineConstants");
        using var cloud = QueryProject("-getProperty:IncludeCloudFeatures,CloudPackageSuffix,DefineConstants", "-p:IncludeCloudFeatures=true");
        var defaultProperties = defaults.RootElement.GetProperty("Properties");
        var cloudProperties = cloud.RootElement.GetProperty("Properties");
        Assert.AreEqual("false", defaultProperties.GetProperty("IncludeCloudFeatures").GetString());
        Assert.AreEqual("", defaultProperties.GetProperty("CloudPackageSuffix").GetString());
        Assert.IsFalse(defaultProperties.GetProperty("DefineConstants").GetString()!.Split(';').Contains("BETTERMODMENU_CLOUD_FEATURES"));
        Assert.AreEqual("_cloud", cloudProperties.GetProperty("CloudPackageSuffix").GetString());
        CollectionAssert.Contains(cloudProperties.GetProperty("DefineConstants").GetString()!.Split(';'), "BETTERMODMENU_CLOUD_FEATURES");
    }

    private static JsonDocument QueryProject(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = RepoRoot
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(ProjectPath);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error + output);
        return JsonDocument.Parse(output);
    }

    private static string ProjectPath => Path.Combine(RepoRoot, "BetterModMenu.csproj");

    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static DotnetResult RunDotnet(
        string command,
        string projectPath,
        string target,
        string uploaderRoot,
        string workshopDir,
        string targetPath,
        string targetFileName,
        string capturePath)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = RepoRoot
        };

        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add(uploaderRoot);
        startInfo.ArgumentList.Add(workshopDir);
        startInfo.ArgumentList.Add(targetPath);
        startInfo.ArgumentList.Add(targetFileName);
        startInfo.Environment["MOD_UPLOADER_CAPTURE"] = capturePath;

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new DotnetResult(process.ExitCode, stdout + stderr);
    }

    private static void BuildFakeUploader(string tempDirectory, string uploaderRoot)
    {
        string projectDirectory = Path.Combine(tempDirectory, "fake-uploader");
        Directory.CreateDirectory(projectDirectory);
        string project = Path.Combine(projectDirectory, "FakeUploader.csproj");
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(projectDirectory, "Program.cs"),
            "File.WriteAllLines(Environment.GetEnvironmentVariable(\"MOD_UPLOADER_CAPTURE\")!, new[] { Environment.CurrentDirectory }.Concat(args));");
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = projectDirectory
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(project);
        startInfo.ArgumentList.Add("--nologo");
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error + output);
        string outputDirectory = Path.Combine(projectDirectory, "bin", "Debug", "net8.0");
        foreach (string file in Directory.EnumerateFiles(outputDirectory))
            File.Copy(file, Path.Combine(uploaderRoot, Path.GetFileName(file)));
        string sourceExecutable = Path.Combine(outputDirectory, OperatingSystem.IsWindows() ? "FakeUploader.exe" : "FakeUploader");
        string targetExecutable = Path.Combine(uploaderRoot, "ModUploader.exe");
        File.Copy(sourceExecutable, targetExecutable);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                targetExecutable,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static string GetUploadPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar)
            ? fullPath + "."
            : fullPath + Path.DirectorySeparatorChar + ".";
    }

    private sealed record DotnetResult(int ExitCode, string Output);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bmm-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
