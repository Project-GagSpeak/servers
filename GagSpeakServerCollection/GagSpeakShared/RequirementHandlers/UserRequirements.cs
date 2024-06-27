namespace GagspeakShared.RequirementHandlers;

public enum UserRequirements
{
    Identified = 0b00000001,        // the user exists
    Moderator = 0b00000010,         // the user is a moderator
    Administrator = 0b00000100,     // the user is an administrator
    TemporaryAccess = 0b00001000,   // the user is temporarily accessing
}
