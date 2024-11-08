using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// This class is incredibly important for the aspect of BDSM. 
/// <para><b>With the introduction of public profiles, chances of cyberbullying through it can and will eventually happen.</b></para>
/// <para> This allows us to identify users doing it, and take action upon them. </para>
/// </summary>
public class UserProfileDataReport
{
    // create a generated key on initialization
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportID { get; set; }


    // store the day / time of the report, & the user reported.
    public DateTime ReportTime { get; set; }
    public string ReportedBase64Picture { get; set; } // snapshot the pic at time of report so they cant remove it later.
    public string ReportedDescription { get; set; } // snapshot the description at time of report so they cant remove it later.

    // store the UID belonging to the reported user as a foreign key.
    [ForeignKey(nameof(ReportedUser))]
    public string ReportedUserUID { get; set; }
    public User ReportedUser { get; set; }


    // store the User and UserUID of the person reporting the profile, so we can punish for abusive reports.
    [ForeignKey(nameof(ReportingUser))]
    public string ReportingUserUID { get; set; }
    public User ReportingUser { get; set; }


    // store the reason for the report.
    public string ReportReason { get; set; }
}