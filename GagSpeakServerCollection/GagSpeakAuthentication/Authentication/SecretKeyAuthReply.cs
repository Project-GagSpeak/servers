namespace GagspeakAuthentication;

/// <summary> 
///     Contains information about the authentication result when fetching a secret key.
/// </summary>
/// <param name="Success"> Indicates whether the authentication was successful </param>
/// <param name="Uid"> The ProfileUID </param>
/// <param name="AccountUid"> The AccountUID </param>
/// <param name="TempBan"> If User is temp banned </param>
/// <param name="Permaban"> If User is perma banned </param>
public record SecretKeyAuthReply(bool Success, string Uid, string AccountUid, string Alias, bool TempBan, bool Permaban);