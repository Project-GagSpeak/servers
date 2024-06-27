using Discord;
using Discord.Interactions;

namespace GagSpeakDiscord.Modules.Popups;

public class VerificationModal : IModal
{
    public string Title => "Validate Verification Code";

    [InputLabel("Paste Verification Code here")]
    [ModalTextInput("Verify_string", TextInputStyle.Short, "paste code from plugin UI...")]
    public string VerificationCodeStr { get; set; } // provide the modal with the secret key your account generated upon creation.
    public string CodeToCheckAgainst { get; set; } // the code that the user must enter to verify their account
}
