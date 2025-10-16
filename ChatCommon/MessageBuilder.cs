namespace ChatCommon
{
    public static class MessageBuilder
    {
        private static readonly Dictionary<string, string> _emojiMap = new()
        {
            { "join", "🟢" },
            { "leave", "🔴" },
            { "message", "💬" },
            { "error", "❌" },
            { "success", "✅" },
            { "time", "🕐" },
            { "online", "👥" },
            { "help", "📋" },
            { "system", "⚙️" },
            { "warning", "⚠️" }
        };

        public static string BuildSystemMessage(string message, string type = "system")
        {
            string emoji = _emojiMap.GetValueOrDefault(type, "💡");
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{emoji} [{timestamp}] {message}";
        }

        public static string BuildUserJoinedMessage(string username)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["join"]} [{timestamp}] Пользователь **{username}** присоединился к чату";
        }

        public static string BuildUserLeftMessage(string username)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["leave"]} [{timestamp}] Пользователь **{username}** покинул чат";
        }

        public static string BuildUserMessage(string username, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["message"]} [{timestamp}] **{username}**: {message}";
        }

        public static string BuildOnlineUsersMessage(IEnumerable<string> users)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string userList = string.Join(", ", users);
            return $"{_emojiMap["online"]} [{timestamp}] Онлайн ({users.Count()}): {userList}";
        }

        public static string BuildTimeMessage()
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string fullTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return $"{_emojiMap["time"]} [{timestamp}] Серверное время: {fullTime}";
        }

        public static string BuildHelpMessage()
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"""
            {_emojiMap["help"]} [{timestamp}] Доступные команды:
            /online - список пользователей онлайн
            /time   - текущее время сервера  
            /help   - показать эту справку
            /clear  - очистить экран
            /quit   - выйти из чата

            💡 Просто введите текст для отправки сообщения!
            """;
        }

        public static string BuildErrorMessage(string error)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["error"]} [{timestamp}] Ошибка: {error}";
        }

        public static string BuildSuccessMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["success"]} [{timestamp}] {message}";
        }

        public static string BuildWarningMessage(string warning)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{_emojiMap["warning"]} [{timestamp}] Предупреждение: {warning}";
        }
    }
}