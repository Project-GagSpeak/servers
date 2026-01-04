// A global tuple statement for moodle status info.
global using MoodlesStatusInfoOLD = (
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    GagspeakAPI.StatusType Type,
    string Applier,
    bool Dispelable,
    int Stacks,
    bool Persistent,
    int Days,
    int Hours,
    int Minutes,
    int Seconds,
    bool NoExpire,
    bool AsPermanent,
    System.Guid StatusOnDispell,
    string CustomVFXPath,
    bool StackOnReapply,
    int StacksIncOnReapply
    );

global using MoodlesStatusInfo = (
    int Version,
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    string CustomVFXPath,               // What VFX to show on application.
    long ExpireTicks,                   // Permanent if -1, referred to as 'NoExpire' in MoodleStatus
    GagspeakAPI.StatusType Type,        // Moodles StatusType enum.
    int Stacks,                         // Usually 1 when no stacks are used.
    int StackSteps,                     // How many stacks to add per reapplication.
    GagspeakAPI.Modifiers Modifiers,    // What can be customized, casted to uint from Modifiers (Dalamud IPC Rules)
    System.Guid ChainedStatus,          // What status is chained to this one.
    GagspeakAPI.ChainTrigger ChainTrigger,// What triggers the chained status.
    string Applier,                     // Who applied the moodle.
    string Dispeller,                   // When set, only this person can dispel your moodle.
    bool Permanent                      // Referred to as 'Sticky' in the Moodles UI
);
