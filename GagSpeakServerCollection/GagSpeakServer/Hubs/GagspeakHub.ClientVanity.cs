using GagspeakAPI.Chat;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StackExchange.Redis;
using System.Text.Json;

namespace GagspeakServer.Hubs;

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendChat(SentMessage msg)
    {
        // Validate Sender
        if (!string.Equals(msg.Sender.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        return await (msg.ChatID.Kind switch
        {
            GsChatKind.Global => SendGlobalMessage(msg),
            GsChatKind.Direct => SendDirectMessage(msg),
            _ => Task.FromResult(HubResponseBuilder.AwDangIt(GagSpeakApiEc.ChatNotFound)),
        }).ConfigureAwait(false);
    }

    private async Task<HubResponse> SendGlobalMessage(SentMessage msg)
    {
        var trackedMsg = _chatService.LogGlobalMessage(msg);
        await Clients.All.Callback_ChatMessageReceived(trackedMsg).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterGlobalChatMessages);
        return HubResponseBuilder.Yippee();
    }


    // Could modify to just have client add message if successful but whatever.
    private async Task<HubResponse> SendDirectMessage(SentMessage msg)
    {
        // Parse out the targetUid from the message
        var parts = msg.ChatID.ChatId.Split('-');
        var targetUid = string.Equals(parts[0], UserUID, StringComparison.Ordinal) ? parts[1] : parts[0];
        if (targetUid.Equals(UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CannotInteractWithSelf);

        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == targetUid).ConfigureAwait(false) is not { } targetAuth)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        var trackedMsg = _chatService.LogPrivateMessage(msg);
        await Clients.Users([targetAuth.UserUID, UserUID]).Callback_ChatMessageReceived(trackedMsg).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> RoomSendChat(ChatlogMessage message)
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

        if (await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is { } data)
        {
            data.UpdateInfoFromDto(dto.Info);
            DbContext.Update(data);
        }
        else
        {
            var newData = new UserProfile() { UserUID = dto.User.UID };
            newData.UpdateInfoFromDto(dto.Info);
            await DbContext.UserProfileData.AddAsync(newData).ConfigureAwait(false);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        List<string> allPairsOfCaller = await GetAllPairedUsers().ConfigureAwait(false);
        Dictionary<string, string> onlinePairsOfCaller = await GetOnlineUsers(allPairsOfCaller).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfCaller.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_UserProfileUpdated(new(dto.User)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }


    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserUpdateData(UserDataUpdate dto)
    {
        var newAlias = dto.NewAlias is not null;
        var newVanityName = dto.VanityName is not null;
        var newNameColor = dto.NameColor is not null;
        var newNameGlowColor = dto.NameGlowColor is not null;
        // Check if there's actually anything to update
        if (!newAlias && !newVanityName && !newNameColor && !newNameGlowColor)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Ensure the user exists.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Deny if trying to set SupporterTier content and not a supporter.
        if (user.Tier is CkVanityTier.NoRole && (newVanityName || newNameColor || newNameGlowColor))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidVanityTier);

        // Otherwise we can update the data.
        if (newAlias && !IsValidName(dto.NewAlias))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        if (newVanityName && !IsValidVanityName(dto.VanityName))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        if (newAlias)
        {
            if (await DbContext.Users.AsNoTracking().AnyAsync(u => u.Alias == dto.NewAlias).ConfigureAwait(false))
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyExists);
            user.Alias = dto.NewAlias!.Length is 0 ? null : dto.NewAlias;
        }

        if (newVanityName)
        {
            if (await DbContext.Users.AsNoTracking().AnyAsync(u => u.DisplayName == dto.VanityName).ConfigureAwait(false))
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyExists);
            user.DisplayName = dto.VanityName!.Length is 0 ? null : dto.VanityName;
        }

        if (newNameColor)
        {
            // uint.MinValue acts as a removal indicator
            if (dto.NameColor!.Value == uint.MinValue)
                user.NameColor = null;
            // Otherwise update and force opaque color
            else
                user.NameColor = (dto.NameColor!.Value & 0x00FFFFFF) | 0xFF000000;
        }

        if (newNameGlowColor)
            user.NameGlowColor = dto.NameGlowColor == uint.MinValue ? null : dto.NameGlowColor;

        DbContext.Update(user);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Inform others of the changes.
        var pairsOfCaller = await GetAllPairedUsers().ConfigureAwait(false);
        var onlineUsers = await GetOnlineUsers(pairsOfCaller).ConfigureAwait(false);
        await Clients.Users(onlineUsers.Keys).Callback_UserVanityUpdated(new(user.ToUserData())).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();

#pragma warning disable CS8632
        bool IsValidName(string? value)
        {
            // An empty string is valid because it represents a request to clear the data.
            if (string.IsNullOrEmpty(value))
                return true;
            return value.Length is >= 4 and <= 15 && value.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
        }

        bool IsValidVanityName(string? value)
        {
            if (user.Tier is CkVanityTier.KinkporiumMistress)
                return true;
            // An empty string is valid because it represents a request to clear the data.
            if (string.IsNullOrEmpty(value))
                return true;
            return value.Length is >= 4 and <= 10 && value.All(c => char.IsLetterOrDigit(c));
        }
#pragma warning restore CS8632
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
        UserProfile existingData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);

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
            UserProfile userProfileData = new UserProfile() { UserUID = dto.User.UID };
            userProfileData.Base64ProfilePic = dto.ImageBase64;
            await DbContext.UserProfileData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // Save DB Changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Fetch all paired user's of the client caller
        List<string> allPairedUsers = await GetAllPairedUsers().ConfigureAwait(false);
        Dictionary<string, string> pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // Inform the client caller and all their pairs that their profile has been updated.
        await Clients.Users(pairs.Select(p => p.Key)).Callback_UserProfileUpdated(new(dto.User)).ConfigureAwait(false);
        await Clients.Caller.Callback_UserProfileUpdated(new(dto.User)).ConfigureAwait(false);
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
        if (await DbContext.ProfileReports.AsNoTracking().AnyAsync(r => r.ReportedUserUID == dto.User.UID && r.ReportingUserUID == UserUID).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyReported);

        // Prevent reporting a profile that no longer exists.
        if (await DbContext.UserProfileData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } profile)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinkPlateNotFound);

        // Report is valid, so construct the report to send off.
        var reportToAdd = new ReportedUserProfile()
        {
            ReportTime = DateTime.UtcNow,
            SnapshotIcon = profile.Base64ProfilePic,
            SnapshotJson = profile.Description,

            ReportingUserUID = UserUID,
            ReportedUserUID = dto.User.UID,
            ReportReason = dto.ReportReason,
        };
        await DbContext.ProfileReports.AddAsync(reportToAdd).ConfigureAwait(false);

        // Mark the profile as flagged and update that as well.
        profile.FlaggedForReport = true;
        DbContext.UserProfileData.Update(profile);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Inform the reporter that the report was successful.
        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Your Report was processed, and now pending validation from CK").ConfigureAwait(false);

        // Push a profile update to all users besides the reported user.
        var pairsOfReportedUser = await GetAllPairedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairs = await GetOnlineUsers(pairsOfReportedUser).ConfigureAwait(false);
        IEnumerable<string> onlineUids = onlinePairs.Keys;

        if (!onlineUids.Contains(UserUID, StringComparer.Ordinal))
            await Clients.Caller.Callback_UserProfileUpdated(new(dto.User)).ConfigureAwait(false);
        await Clients.Users(onlineUids).Callback_UserProfileUpdated(new(dto.User)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinkPlateReportsCreated);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///   Whenever a user is reported for misconduct in a radar chat.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserReportChat(ChatReport dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Prevent reporting non-existent users.
        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == dto.User.UID).ConfigureAwait(false) is not { } auth)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Prevent duplicate reports.
        if (await DbContext.ChatReports.AsNoTracking().AnyAsync(r => r.ChatLogId == dto.Chatlog.ChatId && r.MessageId == dto.MessageId && r.Chat == dto.Chatlog.Kind).ConfigureAwait(false))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "This User has already been reported.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyReported);
        }

        var chatBuffer = dto.Chatlog.Kind is GsChatKind.Global
            ? _chatService.GetGlobalChatHistory(200).ToList()
            : _chatService.GetPrivateChatHistory(dto.Chatlog, 200).ToList();
        var targetIndex = chatBuffer.FindIndex(m => string.Equals(m.MsgId, dto.MessageId, StringComparison.Ordinal));
        if (targetIndex is -1)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ChatNotFound);

        // Otherwise collect the message context.
        var contextStartIdx = Math.Max(0, targetIndex - 25);
        int count = Math.Min(chatBuffer.Count - contextStartIdx, 30);
        var chatContext = chatBuffer.GetRange(contextStartIdx, count);

        // Create the chat context.
        var chatMsgsContext = chatContext.Select(m => new JsonChatMsg(m.MsgId, m.TimeSentUTC, m.Sender.UID, m.Sender.AliasOrUID, m.Message)).ToArray();
        var chatMsgsJson = JsonSerializer.Serialize(chatMsgsContext, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = false });

        var reportToAdd = new ReportedChat()
        {
            Chat = dto.Chatlog.Kind,
            ReportTime = DateTime.UtcNow,

            ChatLogId = dto.Chatlog.ChatId,
            MessageId = dto.MessageId,
            Message = chatBuffer[targetIndex].Message ?? string.Empty,
            ChatContextJson = chatMsgsJson,

            ReportedUserUID = dto.User.UID,
            ReporterUID = UserUID,
            ReportReason = dto.Reason,
        };

        await DbContext.ChatReports.AddAsync(reportToAdd).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Inform the reporter that the report was successful.
        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Your Report was processed, and now pending validation from CK").ConfigureAwait(false);

        // If there are more than 2-3ish reports on this user we should flag access immidiately.
        if (await DbContext.ChatReports.AsNoTracking().CountAsync(r => r.ReportedUserUID == dto.User.UID && r.Chat == dto.Chatlog.Kind).ConfigureAwait(false) > 2)
        {
            // Temp disable chat for the reported user for a few hours, giving time for moderation team to act and preventing further disruption.
            auth.AccountRep.ChatTimeout = DateTime.UtcNow.AddHours(2);
            DbContext.Auth.Update(auth);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await Clients.User(dto.User.UID).Callback_ReputationInfo(auth.AccountRep.ToApi(), string.Empty).ConfigureAwait(false);
        }

        var pairsOfReported = await GetAllPairedUsers(dto.User.UID).ConfigureAwait(false);
        var onlineUsers = await GetOnlineUsers(pairsOfReported).ConfigureAwait(false);
        var reportKind = dto.Chatlog.Kind is GsChatKind.Global ? ReportKind.GlobalChat : ReportKind.DMs;
        await Clients.Users([.. onlineUsers.Keys]).Callback_UserFlaggedForReport(reportKind, dto.User.UID).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterChatReportsCreated);
        return HubResponseBuilder.Yippee();
    }
}