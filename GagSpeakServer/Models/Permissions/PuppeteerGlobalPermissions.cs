using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class PuppeteerGlobalPermissions // the global permissions
{
     public bool EnablePuppeteer { get; set; } // if the user's wardrobe component is active
     public string GlobalTriggerPhrase { get; set; } // the global trigger phrase for the user
     public bool GlobalAllowSitRequests { get; set; } // if the user allows sit requests
     public bool GlobalAllowMotionRequests { get; set; } // if the user allows motion requests
     public bool GlobalAllowAllRequests { get; set; } // if the user allows all requests

     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}