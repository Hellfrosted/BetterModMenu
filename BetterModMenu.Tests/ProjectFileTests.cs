using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BetterModMenu.Tests;

[TestClass]
public class ProjectFileTests
{
    [TestMethod]
    public void RunSteamWorkshopUpload_StagesContentAndRunsUploaderFromUploaderRoot()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("This test uses a shell-script fake ModUploader.exe.");
        }

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
        WriteFakeUploader(Path.Combine(uploaderRoot, "ModUploader.exe"));

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
        WriteFakeUploader(Path.Combine(uploaderRoot, "ModUploader.exe"));

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("This test uses a shell-script fake ModUploader.exe.");
        }

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
        WriteFakeUploader(Path.Combine(uploaderRoot, "ModUploader.exe"));

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
        string project = File.ReadAllText(ProjectPath);

        StringAssert.Contains(project, "<Target Name=\"UploadSteamWorkshop\" DependsOnTargets=\"Build;RunSteamWorkshopUpload\" />");
        Assert.IsFalse(project.Contains("AfterTargets=\"Build\" Condition=\"'$(UploadSteamWorkshop)'", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("BeforeTargets=\"Build\" Condition=\"'$(UploadSteamWorkshop)'", StringComparison.Ordinal));
    }

    [TestMethod]
    public void LocalizationCatalogs_AreEmbeddedIntoMainAssembly()
    {
        string project = File.ReadAllText(ProjectPath);

        StringAssert.Contains(project, "<EmbeddedResource Include=\"Localization\\*.json\" />");
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

    private static void WriteFakeUploader(string path)
    {
        File.WriteAllText(
            path,
            """
            #!/usr/bin/env bash
            {
              pwd
              printf '%s\n' "$@"
            } > "$MOD_UPLOADER_CAPTURE"
            """);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
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
