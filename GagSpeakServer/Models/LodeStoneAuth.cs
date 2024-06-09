using System.ComponentModel.DataAnnotations;

namespace GagspeakServer.Models;

/// <summary>
/// <para> At the moment from current analysis this is meant to represent the model of the lodestone auth table that is registered with the discord bot. </para>
/// <para> the current purpose of this for mares goals is to have the lodestone ID and discord ID corrispond to a user and auth string, to generate a UID. </para>
/// <para> In the future, we want to evolve this so that the purpose is to bind an account key's owndership for recovery (if needed). </para>
/// </summary>
public class LodeStoneAuth
{
    [Key]
    public ulong DiscordId { get; set; }
    [MaxLength(100)]
    public string HashedLodestoneId { get; set; }
    [MaxLength(100)]
    public string? LodestoneAuthString { get; set; }
    public User? User { get; set; }
    public DateTime? StartedAt { get; set; }
}
