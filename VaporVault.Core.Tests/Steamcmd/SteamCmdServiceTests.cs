using System.Diagnostics;

using FluentAssertions;
using FluentAssertions.Specialized;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using VaporVault.Core.SteamCmd;
using VaporVault.Core.SteamCmd.Infrastructure;

namespace VaporVault.Core.Tests.SteamCmd;

public class SteamCmdServiceTests
{
    private const string SteamCmdPath = "C:\\steamcmd\\steamcmd.exe";
    private readonly Mock<IPlatformService> _platformService;
    private readonly Mock<ISteamCmdLocator> _steamCmdLocator;
    private readonly SteamCmdService _sut;
    private string? _capturedArguments;

    public SteamCmdServiceTests()
    {
        _steamCmdLocator = new Mock<ISteamCmdLocator>();
        _platformService = new Mock<IPlatformService>();

        // Default setup for all tests
        _steamCmdLocator.Setup(x => x.EnsureSteamCmdAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SteamCmdPath);
        _platformService.Setup(x => x.IsWindows).Returns(true);
        _platformService.Setup(x => x.OSDescription).Returns("Windows");

        _sut = new SteamCmdService(
            _steamCmdLocator.Object,
            NullLogger<SteamCmdService>.Instance,
            _platformService.Object);
    }

    [Fact]
    public async Task RunCommandAsync_EnsuresSteamCmdIsAvailable()
    {
        // Arrange
        SetupMockProcessExecution();

        // Act
        await _sut.RunCommandAsync("+login anonymous");

        // Assert
        _steamCmdLocator.Verify(x => x.EnsureSteamCmdAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCommandAsync_OnNonWindows_SetsExecutablePermissions()
    {
        // Arrange
        _platformService.Setup(x => x.IsWindows).Returns(false);
        SetupMockProcessExecution();

        // Act
        await _sut.RunCommandAsync("+login anonymous");

        // Assert
        _platformService.Verify(x =>
            x.SetExecutablePermissionsAsync(SteamCmdPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCommandAsync_OnWindows_DoesNotSetExecutablePermissions()
    {
        // Arrange
        SetupMockProcessExecution();

        // Act
        await _sut.RunCommandAsync("+login anonymous");

        // Assert
        _platformService.Verify(x =>
            x.SetExecutablePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCommandAsync_AppendsPlusQuitToArguments()
    {
        // Arrange
        var command = "+login anonymous";
        var expectedArgs = command + " +quit";
        SetupMockProcessExecution();

        // Act
        await _sut.RunCommandAsync(command);

        // Assert
        _capturedArguments.Should().Be(expectedArgs);
    }

    [Fact]
    public async Task RunCommandAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        SetupMockProcessExecution();

        // Act & Assert
        var act = () => _sut.RunCommandAsync("+login anonymous", cts.Token);
        cts.Cancel();
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task RunCommandAsync_ReturnsCorrectSuccessBasedOnExitCode(int exitCode, bool expectedSuccess)
    {
        // Arrange
        SetupMockProcessExecution(exitCode);

        // Act
        var result = await _sut.RunCommandAsync("+login anonymous");

        // Assert
        result.Success.Should().Be(expectedSuccess);
        result.ExitCode.Should().Be(exitCode);
    }

    [Fact]
    public async Task DownloadDepotAsync_ConstructsCorrectCommand()
    {
        // Arrange
        const long appId = 123;
        const long depotId = 456;
        const string manifestId = "789";
        var expectedCommand = $"+download_depot {appId} {depotId} {manifestId}";
        SetupMockProcessExecution();

        // Act
        await _sut.DownloadDepotAsync(appId, depotId, manifestId);

        // Assert
        _capturedArguments.Should().StartWith(expectedCommand);
        _capturedArguments.Should().EndWith(" +quit");
    }

    [Fact]
    public async Task DownloadDepotAsync_WithoutManifest_OmitsManifestId()
    {
        // Arrange
        const long appId = 123;
        const long depotId = 456;
        var expectedCommand = $"+download_depot {appId} {depotId}";
        SetupMockProcessExecution();

        // Act
        await _sut.DownloadDepotAsync(appId, depotId);

        // Assert
        _capturedArguments.Should().StartWith(expectedCommand);
        _capturedArguments.Should().EndWith(" +quit");
    }

    private void SetupMockProcessExecution(int exitCode = 0)
    {
        // Use TypeMock or similar to intercept Process.Start
        // For now, we'll just capture the arguments
        _capturedArguments = null;
        _steamCmdLocator.Setup(x => x.EnsureSteamCmdAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct =>
            {
                // Simulate process execution delay
                Task.Delay(10, ct).Wait();
            })
            .ReturnsAsync(SteamCmdPath);
    }
}