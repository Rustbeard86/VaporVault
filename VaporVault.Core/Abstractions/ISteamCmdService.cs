using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VaporVault.Core.SteamCmd;

namespace VaporVault.Core.Abstractions
{
    public interface ISteamCmdService
    {
        Task<SteamCmdResult> RunCommandAsync(string arguments, CancellationToken cancellationToken = default);
        Task<SteamCmdResult> DownloadDepotAsync(long appId, long depotId, string manifestId = null, CancellationToken cancellationToken = default);
    }

}
