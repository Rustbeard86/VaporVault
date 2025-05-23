namespace VaporVault.Core.SteamCmd.Exceptions;

public class SteamCmdException : Exception
{
    public SteamCmdException(string message) : base(message)
    {
    }

    public SteamCmdException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class SteamCmdDownloadException : SteamCmdException
{
    public SteamCmdDownloadException(string message) : base(message)
    {
    }

    public SteamCmdDownloadException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class SteamCmdExtractionException : SteamCmdException
{
    public SteamCmdExtractionException(string message) : base(message)
    {
    }

    public SteamCmdExtractionException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class SteamCmdValidationException : SteamCmdException
{
    public SteamCmdValidationException(string message) : base(message)
    {
    }

    public SteamCmdValidationException(string message, Exception inner) : base(message, inner)
    {
    }
}