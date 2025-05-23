using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using VaporVault.Core.SteamCmd;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation;
using VaporVault.Core.SteamCmd.Validation.Events;

namespace VaporVault.Core.Tests.Steamcmd;

/// <summary>
///     Unit tests for basic SteamCmdLocator functionality
/// </summary>
public class SteamCmdLocatorTests
{
    private const string BaseDir = "C:\\steamcmd";
    private const string ExePath = "C:\\steamcmd\\steamcmd.exe";
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<IPlatformService> _platformService;
    private readonly Mock<ISteamCmdValidator> _validator; // Add validator mock
    private readonly ISteamCmdLocator _sut;

    public SteamCmdLocatorTests()
    {
        _fileSystem = new Mock<IFileSystem>();
        _platformService = new Mock<IPlatformService>();
        _validator = new Mock<ISteamCmdValidator>(); // Initialize validator mock

        var options = new SteamCmdDownloadOptions
        {
            InstallDirectory = BaseDir,
            ForceRedownload = false
        };

        // Basic setup needed for all tests
        _platformService.Setup(x => x.IsWindows).Returns(true);
        _fileSystem.Setup(x => x.Combine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        _sut = new SteamCmdLocator(
            NullLogger<SteamCmdLocator>.Instance,
            _fileSystem.Object,
            _platformService.Object,
            Mock.Of<IHttpDownloadService>(),
            _validator.Object, // Add validator
            options);
    }

    [Fact]
    public void GetInstalledSteamCmdPath_WhenNotYetEnsured_ReturnsNull()
    {
        _sut.GetInstalledSteamCmdPath().Should().BeNull();
    }

    [Fact]
    public async Task GetInstalledSteamCmdPath_AfterEnsure_ReturnsCachedPath()
    {
        // Arrange
        _fileSystem.Setup(x => x.FileExists(ExePath)).Returns(true);

        // Act
        await _sut.EnsureSteamCmdAvailableAsync();
        var result = _sut.GetInstalledSteamCmdPath();

        // Assert
        result.Should().Be(ExePath);
    }
}

/// <summary>
///     Integration tests for actual SteamCmd download and extraction
/// </summary>
[Trait("Category", "Integration")]
public class SteamCmdLocatorIntegrationTests : IDisposable
{
    private readonly ISteamCmdLocator _sut;
    private readonly string _testDir;

    public SteamCmdLocatorIntegrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SteamCmdTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        var options = new SteamCmdDownloadOptions
        {
            InstallDirectory = _testDir,
            ForceRedownload = true
        };

        // Create validator for integration tests
        var validator = new SteamCmdValidator(
            new SteamCmd.Infrastructure.FileSystem(),
            new PlatformService(),
            new LoggingValidationEventHandler(NullLogger.Instance));

        _sut = new SteamCmdLocator(
            NullLogger<SteamCmdLocator>.Instance,
            new SteamCmd.Infrastructure.FileSystem(),
            new PlatformService(),
            new HttpDownloadService(),
            validator, // Add validator
            options);
    }

    public void Dispose()
    {
        try
        {
            // Cleanup our specific test directory
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);

            // Cleanup parent if empty
            var parentDir = Path.GetDirectoryName(_testDir);
            if (parentDir != null && Directory.Exists(parentDir)
                                  && !Directory.EnumerateFileSystemEntries(parentDir).Any())
            {
                Directory.Delete(parentDir);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task EnsureSteamCmdAvailable_DownloadsAndExtractsFile()
    {
        // Act
        var result = await _sut.EnsureSteamCmdAvailableAsync();

        // Assert
        result.Should().NotBeNull();
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSteamCmdAvailable_WhenCalledTwice_ReusesCachedFile()
    {
        // Act
        var firstResult = await _sut.EnsureSteamCmdAvailableAsync();
        var secondResult = await _sut.EnsureSteamCmdAvailableAsync();

        // Assert
        secondResult.Should().Be(firstResult);
        File.Exists(firstResult).Should().BeTrue();
    }
}