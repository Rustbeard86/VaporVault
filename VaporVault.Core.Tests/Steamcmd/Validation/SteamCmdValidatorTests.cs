using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VaporVault.Core.SteamCmd.Exceptions;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation;
using VaporVault.Core.SteamCmd.Validation.Events;

namespace VaporVault.Core.Tests.Steamcmd.Validation;

public class SteamCmdValidatorTests
{
    private readonly Mock<IValidationEventHandler> _eventHandler;
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<IPlatformService> _platformService;
    private readonly ISteamCmdValidator _sut;

    public SteamCmdValidatorTests()
    {
        _fileSystem = new Mock<IFileSystem>();
        _platformService = new Mock<IPlatformService>();
        _eventHandler = new Mock<IValidationEventHandler>();

        _sut = new SteamCmdValidator(
            _fileSystem.Object,
            _platformService.Object,
            _eventHandler.Object);
    }

    [Fact]
    public async Task ValidateArchiveAsync_WhenZipArchiveIsValid_DoesNotThrow()
    {
        // Arrange
        var archivePath = "test.zip";
        var archiveData = CreateValidZipArchive();
        SetupFileSystem(archivePath, archiveData);

        // Act
        await _sut.ValidateArchiveAsync(archivePath, CancellationToken.None);

        // Assert
        _eventHandler.Verify(x =>
            x.HandleEvent(It.Is<ValidationEvent>(e =>
                e.Level == LogLevel.Information && e.Message.Contains("successful"))));
    }

    [Fact]
    public async Task ValidateArchiveAsync_WhenZipArchiveIsEmpty_ThrowsException()
    {
        // Arrange
        var archivePath = "empty.zip";
        var emptyArchive = CreateEmptyZipArchive();
        SetupFileSystem(archivePath, emptyArchive);

        // Act
        var act = () => _sut.ValidateArchiveAsync(archivePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("Archive contains no entries");
        _eventHandler.Verify(x =>
            x.HandleEvent(It.Is<ValidationEvent>(e => e.Level == LogLevel.Error && e.Message.Contains("no entries"))));
    }

    [Fact]
    public async Task ValidateExecutableAsync_WhenWindowsExeIsValid_DoesNotThrow()
    {
        // Arrange
        var exePath = "steamcmd.exe";
        var exeData = CreateValidWindowsExecutable();
        SetupFileSystem(exePath, exeData);
        _platformService.Setup(x => x.IsWindows).Returns(true);

        // Act
        await _sut.ValidateExecutableAsync(exePath, CancellationToken.None);

        // Assert
        _eventHandler.Verify(x =>
            x.HandleEvent(It.Is<ValidationEvent>(e =>
                e.Level == LogLevel.Information && e.Message.Contains("successful"))));
    }

    [Fact]
    public async Task ValidateExecutableAsync_WhenWindowsExeIsInvalid_ThrowsException()
    {
        // Arrange
        var exePath = "invalid.exe";
        var invalidData = new byte[] { 0x00, 0x00 }; // Not MZ header
        SetupFileSystem(exePath, invalidData);
        _platformService.Setup(x => x.IsWindows).Returns(true);

        // Act
        var act = () => _sut.ValidateExecutableAsync(exePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("File is not a valid Windows executable");
    }

    [Fact]
    public async Task EnsureSufficientDiskSpaceAsync_WhenSpaceIsAvailable_DoesNotThrow()
    {
        // Arrange
        var path = @"C:\test";
        var requiredBytes = 1024L * 1024L; // 1MB

        // Act
        await _sut.EnsureSufficientDiskSpaceAsync(path, requiredBytes, CancellationToken.None);

        // Assert
        _eventHandler.Verify(x =>
            x.HandleEvent(It.Is<ValidationEvent>(e =>
                e.Level == LogLevel.Information && e.Message.Contains("Sufficient"))));
    }

    [Fact]
    public async Task ValidateArchiveAsync_WhenFileDoesNotExist_ThrowsException()
    {
        // Arrange
        var archivePath = "nonexistent.zip";
        _fileSystem.Setup(x => x.FileExists(archivePath)).Returns(false);
        _fileSystem.Setup(x => x.OpenRead(archivePath))
            .Throws(new FileNotFoundException($"Could not find file '{archivePath}'"));

        // Act
        var act = () => _sut.ValidateArchiveAsync(archivePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("Archive file not found at nonexistent.zip");

        _eventHandler.Verify(x => x.HandleEvent(It.Is<ValidationEvent>(e =>
            e.Level == LogLevel.Error &&
            e.Message == "Archive file not found" &&
            e.Properties["ArchivePath"].ToString() == archivePath)));
    }

    [Fact]
    public async Task ValidateArchiveAsync_WhenFileIsCorrupted_ThrowsException()
    {
        // Arrange
        var archivePath = "corrupted.zip";
        var corruptedData = new byte[] { 0x50, 0x4B, 0x03 }; // Incomplete ZIP header
        SetupFileSystem(archivePath, corruptedData);

        // Act
        var act = () => _sut.ValidateArchiveAsync(archivePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("*Failed to validate archive*");
    }

    [Fact]
    public async Task ValidateExecutableAsync_WhenExecutableIsTruncated_ThrowsException()
    {
        // Arrange
        var exePath = "truncated.exe";
        var truncatedData = new byte[] { (byte)'M' }; // Incomplete MZ header
        SetupFileSystem(exePath, truncatedData);
        _platformService.Setup(x => x.IsWindows).Returns(true);

        // Act
        var act = () => _sut.ValidateExecutableAsync(exePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("*File is too small*");
    }

    [Fact]
    public async Task ValidateArchiveAsync_WhenArchiveHasInvalidFormat_ThrowsException()
    {
        // Arrange
        var archivePath = "invalid.xyz";
        _fileSystem.Setup(x => x.FileExists(archivePath)).Returns(true);  // File exists but wrong format

        // Act
        var act = () => _sut.ValidateArchiveAsync(archivePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("Unsupported archive format for SteamCMD");

        _eventHandler.Verify(x => x.HandleEvent(It.Is<ValidationEvent>(e =>
            e.Level == LogLevel.Error &&
            e.Message == "Unsupported archive format")));
    }

    [Fact]
    public async Task ValidateExecutableAsync_WhenFilePermissionDenied_ThrowsException()
    {
        // Arrange
        var exePath = "noaccess.exe";
        _fileSystem.Setup(x => x.FileExists(exePath)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(exePath))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var act = () => _sut.ValidateExecutableAsync(exePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SteamCmdValidationException>()
            .WithMessage("*Failed to validate executable*")
            .Where(ex => ex.InnerException is UnauthorizedAccessException);
    }

    private void SetupFileSystem(string path, byte[] fileContent)
    {
        var stream = new MemoryStream(fileContent);
        _fileSystem.Setup(x => x.FileExists(path)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(path)).Returns(stream);
    }

    private static byte[] CreateValidZipArchive()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("test.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("Test content");
        }

        return memoryStream.ToArray();
    }

    private static byte[] CreateEmptyZipArchive()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Create empty archive
        }

        return memoryStream.ToArray();
    }

    private static byte[] CreateValidWindowsExecutable()
    {
        // Create minimal DOS MZ header
        return new byte[] { (byte)'M', (byte)'Z', 0x00, 0x00 };
    }
}