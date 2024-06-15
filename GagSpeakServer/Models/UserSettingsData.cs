using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagSpeakServer.Models.Permissions;
using Gagspeak.API.Data.Enum;
namespace GagspeakServer.Models;

public class UserSettingsData
{
     public string Safeword { get; set; }           // the user's safeword
     public bool SafewordUsed { get; set; }         // if the safeword has been used
     public bool CmdsFromFriends { get; set; }      // if commands can be sent from friends (will likely need a full rework)
     public bool CmdsFromParty { get; set; }        // if commands can be sent from party members (will likely need a full rework)
     public bool DirectChatGarblerActive { get; set; } // if the user has direct chat garbler active
     public bool DirectChatGarblerLocked { get; set; } // if the user has direct chat garbler locked
     public bool LiveGarblerZoneChangeWarn { get; set; } // if user wants to be warned about the live chat garbler on zone change
     public RevertStyle RevertStyle { get; set; }   // how the user wants to revert their settings (can store locally?)
     public UserApperanceData UserApperanceData { get; set; } // the user's appearance data information
     public WardrobeGlobalPermissions WardrobeGlobalPermissions { get; set; } // the global permissions for the wardrobe module
     public PuppeteerGlobalPermissions PuppeteerGlobalPermissions { get; set; } // the global permissions for the puppeteer module
     public ToyboxGlobalPermissions ToyboxGlobalPermissions { get; set; }       // the global permissions for the toybox module
     /*     public User User { get; set; }               // the user profile this profiledata is for         */

     // the UserUID is a foreign key to the User table
     // this means that the UserUID must be a primary key in the User table,
     // and also that the UserUID must be unique in the User table
     // (This is also the primary key for this class)
     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}