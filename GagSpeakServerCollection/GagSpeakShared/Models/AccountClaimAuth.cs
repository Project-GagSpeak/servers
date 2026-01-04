using System.ComponentModel.DataAnnotations;
#nullable enable

namespace GagspeakShared.Models;

/// <summary> 
///     Created after a <see cref="User"/>'s <see cref="Auth"/> model is correctly verified. <para />
///     This process is assigned by the discord bot after verification is complete. <br/>
///     Access this model when needing to validate a user.
/// </summary>
public class AccountClaimAuth
{
    [Key]
    public ulong DiscordId { get; set; }

    // the secret key generated in the users client that they want to claim
    [MaxLength(100)]
    public string? InitialGeneratedKey { get; set; }

    // the string that we will send to the user's client connected under the initial key
    [MaxLength(100)]
    public string? VerificationCode { get; set; }

    // the user profile which the discord user desires to claim ownership of
    public User? User { get; set; }

    // the time the claim process was started
    public DateTime? StartedAt { get; set; }
}
