using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System.IO;
using System.Text.Json;

namespace Alissa.Core.Services
{
    public class SessionManager : ISessionManager
    {
        // Constants
        private const string DATA_DIR = "data";
        private const string SESSIONS_DIR = "sessions";
        private const string JSON_EXTENSION = ".json";

        private readonly string _sessionsDir;

        public SessionManager(string basePath)
        {
            _sessionsDir = Path.Combine(basePath, DATA_DIR, SESSIONS_DIR);
            Directory.CreateDirectory(_sessionsDir);
        }

        public Session CreateSession()
        {
            Session newSession = new Session();
            return newSession;
        }

        public void SaveSession(Session session)
        {
            string file = Path.Combine(_sessionsDir, session.SessionId + JSON_EXTENSION);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(session, options);
            File.WriteAllText(file, json);
        }

        public Session? LoadSession(string sessionId)
        {
            string file = Path.Combine(_sessionsDir, sessionId + JSON_EXTENSION);
            bool fileExists = File.Exists(file);
            if (!fileExists)
            {
                return null;
            }

            string json = File.ReadAllText(file);
            Session? session = JsonSerializer.Deserialize<Session>(json);
            return session;
        }
    }
}
