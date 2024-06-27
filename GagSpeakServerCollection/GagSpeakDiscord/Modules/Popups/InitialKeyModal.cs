using Discord;
using Discord.Interactions;

namespace GagSpeakDiscord.Modules.Popups;

public class InitialKeyModal : IModal
{
    public string Title => "Claim Account Via Initial Key";

    [InputLabel("Input Secret Key of Account")]
    [ModalTextInput("Initialkey_string", TextInputStyle.Short, "Paste account secret key here...!")]
    public string InitialKeyStr { get; set; } // provide the modal with the secret key your account generated upon creation.
}