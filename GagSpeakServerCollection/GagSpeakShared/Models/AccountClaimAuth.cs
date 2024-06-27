using System.ComponentModel.DataAnnotations;
#nullable enable

namespace GagspeakShared.Models;

/// <summary> AccountClaimAuth class is used to provide and verify the authentication of a user wishing to claim their account.
/// <list type="bullet">
/// <item>
/// <description>User will enter the initial generated key in the client plugin, requesting to claim ownership of it</description>
/// </item>
/// <item>
/// <description>Discord bot sends request to server, asking it to send the verification code to the connected client under that key.</description>
/// </item>
/// <item>
/// <description>Client receives the verification code types it into the discord bot prompt field</description>
/// </item>
/// <item>
/// <description>If verification code and entered code match, account can be claimed.</description>
/// </item>
/// </list>
/// </summary
public class AccountClaimAuth
{
    [Key]
    public ulong DiscordId { get; set; }

    [MaxLength(100)] // probably wise to hash this secret key as well
    public string? InitialGeneratedKey { get; set; }   // the secret key generated in the users client that they want to claim

    [MaxLength(100)]
    public string? VerificationCode { get; set; }     // the string that we will send to the user's client connected under the initial key

    public User? User { get; set; }                   // the user profile which the discord user desires to claim ownership of

    public DateTime? StartedAt { get; set; }          // the time the claim process was started
}
