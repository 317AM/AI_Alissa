using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System.IO;
using System.Text.Json;

namespace Alissa.Core.Services
{
    public class SessionManager : ISessionManager
    {
        private readonly string _sessionsDir;

        public SessionManager(string basePath)
        {
            _sessionsDir = Path.Combine(basePath, "data", "sessions");
            Directory.CreateDirectory(_sessionsDir);
        }

        public Session CreateSession()
        {
            return new Session();
        }

        public void SaveSession(Session session)
        {
            string file = Path.Combine(_sessionsDir, session.SessionId + ".json");
            string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }

        public Session? LoadSession(string sessionId)
        {
            string file = Path.Combine(_sessionsDir, sessionId + ".json");
            if (!File.Exists(file)) return null;

            string json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<Session>(json);
        }
    }
}
