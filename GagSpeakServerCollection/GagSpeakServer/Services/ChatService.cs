using GagspeakAPI.Chat;

namespace GagspeakServer.Services;

/// <summary>
///   Documents all chat channel history temporarily to help with reporting and fetching recent messages
/// </summary>
public class ChatService
{
    private const int GLOBAL_CHAT_LIMIT = 3000;
    private const int MESSAGE_HISTORY_LIMIT = 250;

    private readonly ILogger<ChatService> _logger;

    private readonly Lock _recorderLock = new();
    private readonly LinkedList<ChatlogMessage> _globalChatHistory = new();
    private readonly Dictionary<ChatlogId, LinkedList<ChatlogMessage>> _chatHistory = [];
    private readonly Dictionary<ChatlogId, DateTime> _recentActivity = []; // For pruning inactive channels, if needed.

    public ChatService(ILogger<ChatService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///   Logs a message specifically to the Global Chat channel.
    /// </summary>
    public ChatlogMessage LogGlobalMessage(SentMessage sentMsg)
    {
        var msgId = Guid.NewGuid().ToString("N"); // No dashes
        var timeSent = DateTime.UtcNow;

        var logMsg = new ChatlogMessage(sentMsg.ChatID, msgId, timeSent, sentMsg.Sender, sentMsg.Message, sentMsg.Contents)
        {
            GlobalChatFlags = sentMsg.GlobalChatFlags
        };

        lock (_recorderLock)
        {
            _globalChatHistory.AddLast(logMsg);
            // Trim excess for global
            if (_globalChatHistory.Count > GLOBAL_CHAT_LIMIT)
                _globalChatHistory.RemoveFirst();
        }
        _logger.LogInformation($"Global ChatMsg Logged. Global chat now has {_globalChatHistory.Count} messages.");
        return logMsg;
    }

    /// <summary>
    ///   Logs a private, room, or sanction message to its specific ChatlogId history.
    /// </summary>
    public ChatlogMessage LogPrivateMessage(SentMessage sentMsg)
    {
        var msgId = Guid.NewGuid().ToString("N");
        var timeSent = DateTime.UtcNow;
        var logMsg = new ChatlogMessage(sentMsg.ChatID, msgId, timeSent, sentMsg.Sender, sentMsg.Message, sentMsg.Contents);

        lock (_recorderLock)
        {
            if (!_chatHistory.TryGetValue(sentMsg.ChatID, out var history))
            {
                history = new LinkedList<ChatlogMessage>();
                _chatHistory[sentMsg.ChatID] = history;
            }

            history.AddLast(logMsg);
            // Trim excess for private chats
            if (history.Count > MESSAGE_HISTORY_LIMIT)
                history.RemoveFirst();
            // Track last activity
            _recentActivity[sentMsg.ChatID] = DateTime.UtcNow;
        }
        return logMsg;
    }

    /// <summary>
    ///   Extracts details from the global chat history.
    /// </summary>
    public IReadOnlyList<ChatlogMessage> GetGlobalChatHistory(int msgCount)
    {
        lock (_recorderLock)
        {
            var toTake = Math.Min(msgCount, _globalChatHistory.Count);
            if (toTake == 0) return [];

            var res = new ChatlogMessage[toTake];
            var node = _globalChatHistory.Last;
            for (int i = toTake - 1; i >= 0 && node is not null; i--)
            {
                var msg = node.Value;
                res[i] = new(msg.ChatID, msg.MsgId, msg.TimeSentUTC, msg.Sender, msg.Message, msg.Contents)
                {
                    GlobalChatFlags = msg.GlobalChatFlags
                };
                node = node.Previous;
            }

            _logger.LogTrace($"Retrieved {toTake} messages from Global chat history.");
            return res;
        }
    }

    /// <summary>
    ///   Extracts details from a specific private/local chat history.
    /// </summary>
    public IReadOnlyList<ChatlogMessage> GetPrivateChatHistory(ChatlogId id, int msgCount)
    {
        lock (_recorderLock)
        {
            if (!_chatHistory.TryGetValue(id, out var history))
                return [];

            var toTake = Math.Min(msgCount, history.Count);
            if (toTake == 0) return [];

            var res = new ChatlogMessage[toTake];
            var node = history.Last;
            for (int i = toTake - 1; i >= 0 && node is not null; i--)
            {
                var msg = node.Value;
                res[i] = new(id, msg.MsgId, msg.TimeSentUTC, msg.Sender, msg.Message, msg.Contents)
                {
                    GlobalChatFlags = msg.GlobalChatFlags,
                };
                node = node.Previous;
            }

            _logger.LogTrace($"Retrieved {toTake} messages from [{id.Kind}]({id.ChatId})'s chat history.");
            return res;
        }
    }

    /// <summary>
    ///   Completely removes a private chat history from memory.
    /// </summary>
    public void ClearChat(ChatlogId id)
    {
        lock (_recorderLock)
        {
            if (_chatHistory.Remove(id))
            {
                _recentActivity.Remove(id);
                _logger.LogDebug($"Cleared chat history for [{id.Kind}]({id.ChatId}).");
            }
        }
    }

    /// <summary>
    ///   Purges private chat histories that have had no activity past the specified inactivity threshold.
    /// </summary>
    public void PurgeInactiveChats(TimeSpan maxInactivity)
    {
        lock (_recorderLock)
        {
            var cutoff = DateTime.UtcNow - maxInactivity;
            var inactiveIds = _recentActivity.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();

            foreach (var id in inactiveIds)
            {
                _chatHistory.Remove(id);
                _recentActivity.Remove(id);
                _logger.LogDebug($"Purged inactive chat history for [{id.Kind}]({id.ChatId}) due to inactivity.");
            }

            if (inactiveIds.Count > 0)
                _logger.LogInformation($"Chat Cleanup: Purged {inactiveIds.Count} inactive chat channels from memory.");
        }
    }
}
