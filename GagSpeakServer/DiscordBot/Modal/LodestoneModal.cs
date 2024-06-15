using Discord;
using Discord.Interactions;

namespace GagSpeakServer.DiscordBot.Modal;

// restructure this with the verification by sending the code to client and stuff. (maybe could delete entirely)

public class LodestoneModal : IModal
{
    public string Title => "Verify with Lodestone";

    [InputLabel("Enter the Lodestone URL of your Character")]
    [ModalTextInput("lodestone_url", TextInputStyle.Short, "https://*.finalfantasyxiv.com/lodestone/character/<CHARACTERID>/")]
    public string? LodestoneUrl { get; set; }
}
