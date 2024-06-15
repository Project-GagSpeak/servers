// Namespace for the Authentication related classes and records in the GagspeakSynchronosAuthService project
namespace GagspeakServer.Authentication;

// Defines a record type SecretKeyAuthReply
// This record is used to encapsulate the response from the secret key authentication process
public record SecretKeyAuthReply(
    bool Success, // Indicates whether the authentication was successful
    string Uid, // The unique identifier of the user
    string PrimaryUid, // The primary unique identifier of the user
    string Alias, // The alias of the user
    bool TempBan, // Indicates whether the user is temporarily banned
    bool Permaban // Indicates whether the user is permanently banned
);