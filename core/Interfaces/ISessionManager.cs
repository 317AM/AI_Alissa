using Alissa.Core.Models;

namespace Alissa.Core.Interfaces
{
    public interface ISessionManager
    {
        Session CreateSession();
        void SaveSession(Session session);
        Session? LoadSession(string sessionId);
    }
}
