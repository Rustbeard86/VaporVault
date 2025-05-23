namespace VaporVault.Core.SteamCmd.Infrastructure;

public interface IFileSystem
{
    bool FileExists(string path);
    void CreateDirectory(string path);
    void DeleteFile(string path);
    Stream OpenRead(string path);
    Stream Create(string path);
    string GetDirectoryName(string path);
    string Combine(params string[] paths);
}