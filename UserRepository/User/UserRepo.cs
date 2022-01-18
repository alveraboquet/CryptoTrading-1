using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UserModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;

namespace UserRepository
{
    public class UserRepo : IUserRepository
    {
        private UserContext _context;
        public string HashPassword(string password)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(password);
            using SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += string.Format("{0:x2}", x);
            }
            return hashString.ToLower();
        }


        public UserRepo(UserContext userContext)
        {
            _context = userContext;
        }
        public async Task<User> Create(User user)
        {
            user.UserName = user.UserName.ToLower();
            user.Email = user.Email.ToLower();
            user.Password = HashPassword(user.Password);

            await _context.Users.AddAsync(user);
            await this.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<User>> Get()
        {
            return await _context.Users.ToListAsync();
        }

        public Task<User> Get(int id)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<int> GetAccountId(string userNameOrEmail, string password)
        {
            userNameOrEmail = userNameOrEmail.ToLower();
            var hashedPass = this.HashPassword(password);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                        u.UserName == userNameOrEmail ||
                        u.Email == userNameOrEmail);

            if (user == default)
                throw new Exception("Doesn't exist username / email");
            else if (user.Password != hashedPass)
                throw new Exception("Wrong password");

            return user.Id;
        }

        public async Task<List<ResUserSession>> GetSessions(int accountId, string accountToken)
        {
            return (await _context.Users.Where(u => u.Id == accountId).Select(u => u.Sessions).FirstOrDefaultAsync())
                .OrderByDescending(s => s.AccessToken == accountToken)
                .Select(s => new ResUserSession()
                {
                    IP = s.IP,
                    LastLogin = s.LastLogin,
                    SessionId = s.Id,
                    Type = s.Type,
                    UserAgent = s.UserAgent
                }).ToList();
        }

        public async Task<User> GetUser(string userNameOrEmail)
        {
            userNameOrEmail = userNameOrEmail.ToLower();
            var user = await (from u in _context.Users.AsQueryable()
                              where (u.Email == userNameOrEmail || u.UserName == userNameOrEmail)
                              select u).FirstOrDefaultAsync();
            return user;
        }

        public async Task<ResUserInfo> GetUserInfo(int accountId)
        {
            return await (from u in _context.Users.AsQueryable()
                          where u.Id == accountId
                          select new ResUserInfo
                          {
                              Email = u.Email,
                              AutoPlanRenew = u.AutoPlanRenew,
                              Plan = u.Plan,
                              PlanExpiration = u.PlanExpiration,
                              StripeMid = u.StripeMid,
                              UserName = u.UserName
                          }).FirstOrDefaultAsync();
        }

        public async Task<bool> IsEmailValid(int accountId, string email)
        {
            email = email.ToLower();
            return await (from u in _context.Users.AsQueryable()
                          where u.Id == accountId && u.Email == email
                          select u).AnyAsync();
        }

        public Task<bool> IsExistSession(int accountId, string accessToken)
        {
            return _context.Users.AnyAsync(u => u.Id == accountId && u.Sessions.Any(s => s.AccessToken == accessToken));
        }

        public Task<bool> IsExistEmail(string email)
        {
            email = email.ToLower();

            return (from u in _context.Users.AsQueryable()
                    where u.Email == email
                    select u).AnyAsync();
        }

        public async Task<bool> IsExistUser(string userName, string email)
        {
            email = email.ToLower();
            userName = userName.ToLower();

            var user = await (from u in _context.Users.AsQueryable()
                              where u.UserName == userName || u.Email == email
                              select new { u.Email, u.UserName }
                             ).FirstOrDefaultAsync();

            if (user != null)
            {
                if (user.UserName == userName) throw new Exception("user name exist");
                if (user.Email == email) throw new Exception("email exist");
            }

            return true;
        }

        public Task<bool> IsExistUserName(string userName)
        {
            userName = userName.ToLower();

            return (from u in _context.Users.AsQueryable()
                    where u.UserName == userName
                    select u).AnyAsync();
        }

        public Task<bool> IsPasswordValid(int accountId, string password)
        {
            password = this.HashPassword(password);

            return (from u in _context.Users.AsQueryable()
                    where u.Id == accountId && u.Password == password
                    select u).AnyAsync();
        }

        public async Task Remove(User User)
        {
            _context.Users.Remove(User);
            await this.SaveChangesAsync();
        }

        public async Task Remove(int id)
        {
            User user = await this.Get(id);
            await this.Remove(user);
        }

        public async Task<bool> RemoveSession(int accountId, long sessionId)
        {
            try
            {
                _context.UserSessions.Remove(await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == accountId));
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemoveSessionByAccountToken(int accountId, string accountToken)
        {
            try
            {
                var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserId == accountId && s.AccessToken == accountToken);
                _context.UserSessions.Remove(session);
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task Update(User user)
        {
            user.UserName = user.UserName.ToLower();
            user.Email = user.Email.ToLower();
            _context.Entry(user).State = EntityState.Modified;
            return this.SaveChangesAsync();
        }

        public async Task<bool> UpdateEmail(int accountId, string newEmail)
        {
            try
            {
                newEmail = newEmail.ToLower();
                User user = new User()
                {
                    Id = accountId,
                    Email = newEmail,
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.Email).IsModified = true;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdatePassword(int accountId, string newPassword)
        {
            try
            {
                newPassword = this.HashPassword(newPassword);
                User user = new User()
                {
                    Id = accountId,
                    Password = newPassword,
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.Password).IsModified = true;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdatePlanAutoRenew(int id, bool val)
        {
            try
            {
                User user = new User()
                {
                    Id = id,
                    AutoPlanRenew = val,
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.AutoPlanRenew).IsModified = true;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserName(int accountId, string newUserName)
        {
            try
            {
                newUserName = newUserName.ToLower();
                User user = new User()
                {
                    Id = accountId,
                    UserName = newUserName,
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.UserName).IsModified = true;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Every user can have a maximum of 15 sessions. 1 User: N Sessions
        /// </summary>
        public async Task<bool> UpsertSessionForNewLogin(int accountId, UserSession session)
        {
            try
            {
                var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserId == accountId &&
                                                s.IP == session.IP &&
                                                s.Type == session.Type &&
                                                s.UserAgent == session.UserAgent);
                if (userSession == default)
                {
                    session.UserId = accountId;
                    _context.UserSessions.Add(session);
                    var sessions = await (from s in _context.UserSessions.AsQueryable()
                                          where s.UserId == accountId
                                          select new
                                          {
                                              s.LastLogin,
                                              s.Id
                                          }).ToListAsync();
                    if (sessions.Count > 14)
                    {
                        long ll = sessions.Min(s => s.LastLogin);
                        long id = sessions.First(s => s.LastLogin == ll).Id;
                        UserSession s = new UserSession() { Id = id };
                        _context.UserSessions.Attach(s);
                        _context.UserSessions.Remove(s);
                    }
                }
                else
                {
                    userSession.IP = session.IP;
                    userSession.UserAgent = session.UserAgent;
                    userSession.Type = session.Type;
                    userSession.LastLogin = session.LastLogin;
                    userSession.AccessToken = session.AccessToken;
                }
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<int> GetPlanAsync(int userId)
        {
            return (from u in _context.Users.AsQueryable()
                    where u.Id == userId
                    select u.Id).FirstOrDefaultAsync();
        }

        public Task<string> GetChartSettings(int userId)
        {
            return (from u in _context.Users.AsQueryable()
                    where u.Id == userId
                    select u.ChartSettings).FirstOrDefaultAsync();
        }


        public async Task<bool> EditChartSettings(int userId, string chartSettings)
        {
            try
            {
                User user = new User()
                {
                    Id = userId,
                    ChartSettings = chartSettings,
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.ChartSettings).IsModified = true;
                await this.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string[]> GetWatchList(int userId)
        {
            return User.GetWatchList(await (from u in _context.Users.AsQueryable()
                                            where u.Id == userId
                                            select u.WatchList).FirstOrDefaultAsync());
        }

        public async Task<string[]> RemoveWatchList(int userId, string exchange, string symbol)
        {
            var user = await _context.Users.FindAsync(userId);
            var res = user.RemoveWatchList(exchange, symbol);
            await this.SaveChangesAsync();
            return res;
        }

        public async Task<string[]> AddWatchList(int userId, string exchange, string symbol)
        {
            var user = await _context.Users.FindAsync(userId);
            var res = user.AddWatchList(exchange, symbol);
            await this.SaveChangesAsync();
            return res;
        }
    }
}
