using System.ComponentModel.DataAnnotations;

namespace GagspeakServer.Models;

/// <summary>
/// <para> At the moment from current analysis this is meant to represent the model of the auth table in the database. </para>
/// <para> This currently associates one account user with one secretkey, and creates a userUID from this. </para>
/// <para> Purpose of everything outside the hashedkey and userUID is still being looked into, however this should change so that one secretkey is able to have multiple UID's.</para>
/// </summary>
public class Auth
{
    [Key]
    [MaxLength(64)]
    public string HashedKey { get; set; }

    public string UserUID { get; set; }
    public User User { get; set; }
    public bool IsBanned { get; set; }
    public string? PrimaryUserUID { get; set; }
    public User? PrimaryUser { get; set; }
}
