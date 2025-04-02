using GagspeakAPI.Data;
using GagspeakShared.Models;
using System.Reflection;

namespace GagspeakServer;

public static class Extensions
{
    public static MoodlesStatusInfo ToStatusInfo(this MoodleStatus status)
    {
        return new MoodlesStatusInfo()
        {
            GUID = status.Identifier,
            IconID = status.IconID,
            Title = status.Title,
            Description = status.Description,
            Type = status.Type,
            Applier = string.Empty,
            Dispelable = status.Dispelable,
            Stacks = status.Stacks,
            Persistent = status.Persistent,
            Days = status.Days,
            Hours = status.Hours,
            Minutes = status.Minutes,
            Seconds = status.Seconds,
            NoExpire = status.NoExpire,
            AsPermanent = status.AsPermanent,
            StatusOnDispell = status.StatusOnDispell,
            CustomVFXPath = status.CustomVFXPath,
            StackOnReapply = status.StackOnReapply,
            StacksIncOnReapply = status.StacksIncOnReapply
        };
    }

    public static PublishedMoodle ToPublishedMoodle(this MoodleStatus status)
    {
        return new PublishedMoodle() { AuthorName = status.Author, MoodleStatus = status.ToStatusInfo() };
    }

    public static PublishedPattern ToPublishedPattern(this PatternEntry pattern)
    {
        return new PublishedPattern()
        {
            Identifier = pattern.Identifier,
            Label = pattern.Name,
            Description = pattern.Description,
            Author = pattern.Author,
            Looping = pattern.ShouldLoop,
            Length = pattern.Length,
            UploadedDate = pattern.TimePublished
        };
    }


    // an extension method for the userData 
    public static UserData ToUserData(this User user)
    {
        return new UserData(user.UID, string.IsNullOrWhiteSpace(user.Alias) ? null : user.Alias, user.VanityTier, user.CreatedDate);
    }

    public static UserData ToUserDataFromUID(this string UserUID)
    {
        return new UserData(UserUID, null, null);
    }

    public static void CopyPropertiesTo<T>(this T source, T target)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source), "Source object is null");
        if (target is null)
            throw new ArgumentNullException(nameof(target), "Target object is null");


        Type type = typeof(T);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                object value = property.GetValue(source, null);
                property.SetValue(target, value, null);
            }
        }
    }
}