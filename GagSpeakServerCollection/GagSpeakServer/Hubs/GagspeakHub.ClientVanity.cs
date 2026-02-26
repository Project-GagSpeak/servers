using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StackExchange.Redis;

namespace GagspeakServer.Hubs;

public partial class GagspeakHub
{
    private const string GagspeakGlobalChat = "GlobalGagspeakChat";

    public async Task<HubResponse> UserSendGlobalChat(ChatMessageGlobal message)
    {
        await Clients.Group(GagspeakGlobalChat).Callback_ChatMessageGlobal(message).ConfigureAwait(false);
        
        _metrics.IncCounter(MetricsAPI.CounterGlobalChatMessages);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> RoomSendChat(ChatMessageVibeRoom message)
    {
        // get the room they are in, in the first place.
        var userRoomKey = VibeRoomRedis.KinksterRoomKey(UserUID);
        // aquire that room name.
        var roomName = await _redis.Database.StringGetAsync(userRoomKey).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get the uid's of the other room participants.
        var uids = (await _redis.Database.SetMembersAsync(VibeRoomRedis.ParticipantsKey(roomName)).ConfigureAwait(false)).ToStringArray();
        await Clients.Users(uids).Callback_RoomChatMessage(message.Sender, message.Message).ConfigureAwait(false);
        
        _metrics.IncCounter(MetricsAPI.CounterVibeLobbyChatsSent);
        return HubResponseBuilder.Yippee();
    }


    /// <summary> Called by the client who wishes to update the database with their latest achievement data. </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserUpdateAchievementData(AchievementsUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto.User));

        // return if the client caller doesnt match the user dto.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // handle case where it was called after a user delete.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // The achievement data can be null, or contain data. If it contains data, we should update it.
        UserAchievementData userSaveData = await DbContext.AchievementData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (userSaveData is not null)
        {
            if (!string.IsNullOrEmpty(dto.AchievementDataBase64))
                userSaveData.Base64AchievementData = dto.AchievementDataBase64;
            else
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }
        else
        {
            UserAchievementData newProfileData = new()
            {
                UserUID = dto.User.UID,
                Base64AchievementData = dto.AchievementDataBase64,
            };
            await DbContext.AchievementData.AddAsync(newProfileData).ConfigureAwait(false);
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Fresh Achievement Data Created").ConfigureAwait(false);
        }

        // Save DB Changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    /// Called by a connected client who wishes to set or update their profile Info.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSetKinkPlateContent(KinkPlateInfo dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto.User));

        // Must be self.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        if (await DbContext.ProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is { } data)
        {
            data.UpdateInfoFromDto(dto.Info);
            DbContext.Update(data);
        }
        else
        {
            var newData = new UserProfileData() { UserUID = dto.User.UID };
            newData.UpdateInfoFromDto(dto.Info);
            await DbContext.ProfileData.AddAsync(newData).ConfigureAwait(false);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        List<string> allPairsOfCaller = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        Dictionary<string, string> onlinePairsOfCaller = await GetOnlineUsers(allPairsOfCaller).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfCaller.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_ProfileUpdated(new(dto.User)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    /// Called by a connected client who wishes to set or update their profile Picture.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSetKinkPlatePicture(KinkPlateImage dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto.User));

        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot modify profile image for anyone but yourself").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // Grab Client Callers current profile data from the database
        UserProfileData existingData = await DbContext.ProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);

        // Grab the new ProfilePictureData if it exists
        if (!string.IsNullOrEmpty(dto.ImageBase64))
        {
            // Convert from the base64 into raw image data bytes.
            byte[] imageData = Convert.FromBase64String(dto.ImageBase64);

            // Load the image into a memory stream
            using MemoryStream ms = new(imageData);

            // Detect format of the image
            SixLabors.ImageSharp.Formats.IImageFormat format = await Image.DetectFormatAsync(ms).ConfigureAwait(false);

            // Ensure it is a png format, throw exception if it is not.
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Your provided image file is not in PNG format").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidImageFormat);
            }

            // Temporarily load the image into memory from the image data to check its ImageSize & FileSize.
            using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

            // Ensure Image meets required parameters.
            if (image.Width > 256 || image.Height > 256)
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Your provided image file is larger than 256x256").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidImageSize);
            }
        }

        // Validate the rest of the profile data.
        if (existingData is not null)
        {
            // update the profile image to the data from the dto.
            existingData.Base64ProfilePic = dto.ImageBase64;
        }
        else // If no data exists, our profile is not yet in the database, so create a fresh one and add it.
        {
            UserProfileData userProfileData = new UserProfileData() { UserUID = dto.User.UID };
            userProfileData.Base64ProfilePic = dto.ImageBase64;
            await DbContext.ProfileData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // Save DB Changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Fetch all paired user's of the client caller
        List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        Dictionary<string, string> pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // Inform the client caller and all their pairs that their profile has been updated.
        await Clients.Users(pairs.Select(p => p.Key)).Callback_ProfileUpdated(new(dto.User)).ConfigureAwait(false);
        await Clients.Caller.Callback_ProfileUpdated(new(dto.User)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }


    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserReportProfile(ProfileReport dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Prevent self-reporting.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CannotInteractWithSelf);

        // Prevent duplicate reports.
        if (await DbContext.ReportedProfiles.AsNoTracking().AnyAsync(r => r.ReportedUserUID == dto.User.UID && r.ReportingUserUID == UserUID).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyReported);

        // Prevent reporting a profile that no longer exists.
        if (await DbContext.ProfileData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } profile)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinkPlateNotFound);

        // Report is valid, so construct the report to send off.
        var reportToAdd = new ReportedProfile()
        {
            ReportTime = DateTime.UtcNow,
            SnapshotImage = profile.Base64ProfilePic,
            SnapshotDescription = profile.Description,

            ReportingUserUID = UserUID,
            ReportedUserUID = dto.User.UID,
            ReportReason = dto.ReportReason,
        };
        await DbContext.ReportedProfiles.AddAsync(reportToAdd).ConfigureAwait(false);

        // Mark the profile as flagged and update that as well.
        profile.FlaggedForReport = true;
        DbContext.ProfileData.Update(profile);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Inform the reporter that the report was successful.
        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Your Report was processed, and now pending validation from CK").ConfigureAwait(false);

        // Push a profile update to all users besides the reported user.
        var pairsOfReportedUser = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairs = await GetOnlineUsers(pairsOfReportedUser).ConfigureAwait(false);
        IEnumerable<string> onlineUids = onlinePairs.Keys;

        if (!onlineUids.Contains(UserUID, StringComparer.Ordinal))
            await Clients.Caller.Callback_ProfileUpdated(new(dto.User)).ConfigureAwait(false);
        await Clients.Users(onlineUids).Callback_ProfileUpdated(new(dto.User)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinkPlateReportsCreated);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///     Whenever a user is reported for misconduct in a radar chat.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserReportChat(ChatReport dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Prevent reporting non-existent users.
        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == dto.User.UID).ConfigureAwait(false) is not { } auth)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "The user you are reporting does not exist.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // Report is valid, so construct the report to send off.
        var reportToAdd = new ReportedChat()
        {
            Type = ReportKind.Chat,
            ReportTime = DateTime.UtcNow,
            CompressedChatHistory = dto.ChatCompressed,

            ReportingUserUID = UserUID,
            ReportedUserUID = dto.User.UID,

            ReportReason = dto.ReportReason,
        };
        await DbContext.ReportedChats.AddAsync(reportToAdd).ConfigureAwait(false);
        // Inform the reporter that the report was successful.
        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Your Report was processed, and now pending validation from CK").ConfigureAwait(false);

        // Disable chat for the reported user until it is resolved.
        auth.AccountRep.ChatUsage = false;
        DbContext.Auth.Update(auth);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterChatReportsCreated);
        return HubResponseBuilder.Yippee();
    }
}