using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class WardrobeGlobalPermissions // the global permissions
{
     public bool EnableWardrobe { get; set; } // if the user's wardrobe component is active
     public bool ItemAutoEquip { get; set; }  // if the user allows items to be auto-equipped
     public bool RestraintSetAutoEquip { get; set; } // if the user allows restraint sets to be auto-equipped
     public bool LockGagStorageOnGagLock { get; set; } // if the user's wardrobe UI is locked

     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}