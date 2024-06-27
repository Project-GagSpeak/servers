
namespace GagspeakAuthentication;

/// <summary> Defines a record type SecretKeyAuthReply
/// <para> This record is used to encapsulate the response from the secret key authentication process </para>
/// </summary>
/// <param name="Success"> Indicates whether the authentication was successful </param>
/// <param name="Uid"> The unique identifier of the user </param>
/// <param name="PrimaryUid"> The primary unique identifier of the user </param>
/// <param name="Alias"> The alias of the user </param>
/// <param name="TempBan"> Indicates whether the user is temporarily banned </param>
/// <param name="Permaban"> Indicates whether the user is permanently banned </param>
public record SecretKeyAuthReply(bool Success, string Uid, string PrimaryUid, string Alias, bool TempBan, bool Permaban);