using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VaporVault.Core.Abstractions;

namespace VaporVault.Core.SteamCmd
{
    public class SteamCmdService : ISteamCmdService
    {
        // Use DI for logger, config, etc.
        public async Task<SteamCmdResult> RunCommandAsync(string arguments, CancellationToken cancellationToken = default)
        {
            // Start steamcmd.exe, capture output, parse result
            // Return typed result, not just raw strings
            throw new NotImplementedException();
        }

        public Task<SteamCmdResult> DownloadDepotAsync(long appId, long depotId, string manifestId = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        // Implement rest of the interface
    }

}
