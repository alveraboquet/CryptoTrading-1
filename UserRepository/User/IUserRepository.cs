using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UserModels;

namespace UserRepository
{
    public interface IUserRepository
    {
        Task<string[]> GetWatchList(int userId);
        Task<string[]> RemoveWatchList(int userId, string exchange, string symbol);
        Task<string[]> AddWatchList(int userId, string exchange, string symbol);


        Task<string> GetChartSettings(int userId);
        Task<bool> EditChartSettings(int userId, string chartSettings);
        Task<int> GetPlanAsync(int userId);
        Task<List<ResUserSession>> GetSessions(int accountId, string accountToken);
        Task<bool> IsExistUserName(string userName);
        Task<bool> IsExistEmail(string email);
        Task<User> Create(User User);
        Task Update(User User);
        Task<IEnumerable<User>> Get();
        Task<ResUserInfo> GetUserInfo(int accountId);
        Task<User> GetUser(string userNameOrEmail);
        Task<bool> UpdatePlanAutoRenew(int id, bool val);
        Task<bool> RemoveSession(int accountId, long sessionId);
        Task<bool> UpsertSessionForNewLogin(int accountId, UserSession session);
        Task<bool> RemoveSessionByAccountToken(int accountId, string accountToken);
        Task<bool> UpdatePassword(int accountId, string newPassword);
        Task<bool> UpdateEmail(int accountId, string newEmail);
        Task<bool> UpdateUserName(int accountId, string newUserName);
        Task<bool> IsPasswordValid(int accountId, string password);
        Task<bool> IsEmailValid(int accountId, string email);


        Task<bool> IsExistUser(string userName, string email);

        Task<int> GetAccountId(string userNameOrEmail, string password);
        Task<User> Get(int id);
        Task Remove(User User);
        Task Remove(int id);
        Task<bool> IsExistSession(int accountId, string accessToken);
        Task SaveChangesAsync();
    }
}
