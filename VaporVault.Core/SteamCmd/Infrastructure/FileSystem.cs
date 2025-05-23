namespace VaporVault.Core.SteamCmd.Infrastructure;

public class FileSystem : IFileSystem
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    public Stream Create(string path)
    {
        return File.Create(path);
    }

    public string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path)!;
    }

    public string Combine(params string[] paths)
    {
        return Path.Combine(paths);
    }
}