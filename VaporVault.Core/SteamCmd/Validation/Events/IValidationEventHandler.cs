namespace VaporVault.Core.SteamCmd.Validation.Events;

public interface IValidationEventHandler
{
    void HandleEvent(ValidationEvent evt);
}