using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class ToyboxGlobalPermissions // the global permissions
{
     public bool EnableToybox { get; set; } // if the user's wardrobe component is active
     public bool LockToyboxUI { get; set; } // if the user's toybox UI is locked
     public bool ToyIsActive { get; set; } // if the user's toy is active
     public int ToyIntensity { get; set; } // the intensity of the user's toy
     public bool UsingSimulatedVibrator { get; set; } // if the user is using a simulated vibrator

     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}