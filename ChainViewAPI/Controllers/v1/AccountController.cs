using Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseRepository;
using ChainViewAPI.Models;
using DataLayer;
using System.Net;
using Wangkanai.Detection.Services;
using Wangkanai.Detection.Models;
using MongoDB.Bson;
using UserModels;
using UserRepository;

namespace ChainViewAPI.Controllers
{
    [Route("v1/api")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly IUserRepository _user;
        private readonly IDetectionService _detectionService;
        public AccountController(IUserRepository user, IDetectionService detectionService)
        {
            _detectionService = detectionService;
            _user = user;
        }

        #region Login Register

        /// <response code="400">enter userName/password</response>
        /// <response code="400">wrong password</response>
        /// <response code="404">Doesn't exist username / email</response>
        /// <response code="200">returns the account-id and account-token</response>
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] ReqLogin request = null)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest("Enter userName (email)");
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Enter password");

            request.UserName = request.UserName.ToLower();

            var user = await _user.GetUser(request.UserName);
            string hashedpass = ((UserRepo)_user).HashPassword(request.Password);
            if (user == default)
                return NotFound("Doesn't exist username / email");
            else if (user.Password != hashedpass)
                return BadRequest("Wrong password");

            UserSession session = GetUserSession();

            await _user.UpsertSessionForNewLogin(user.Id, session);
            //await _user.Update(user);

            ResLoginAndRegister res = new ResLoginAndRegister()
            {
                AccountId = user.Id,
                AccountToken = session.AccessToken,
            };

            return Ok(res);
        }


        /// <response code="400">enter userName/email/password</response>
        /// <response code="400">userName/email exist</response>
        /// <response code="200">returns the account-id and account-token</response>
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] ReqRegister request = null)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest("Enter userName");
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Enter email");
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Enter password");
            request.Email = request.Email.ToLower();
            request.UserName = request.UserName.ToLower();

            try
            {
                await _user.IsExistUser(request.UserName, request.Email);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            User user = new User()
            {
                Password = request.Password,
                Email = request.Email,
                AutoPlanRenew = false,
                PlanExpiration = DateTime.MaxValue.ToUnixTimestamp(),
                UserName = request.UserName,
                Sessions = new List<UserSession>(),
                StripeMid = ""
            };

            UserSession session = GetUserSession();

            user.Sessions.Add(session);
            await _user.Create(user);

            ResLoginAndRegister res = new ResLoginAndRegister()
            {
                AccountId = user.Id,
                AccountToken = session.AccessToken,
            };

            return Ok(res);
        }

        [NonAction]
        private UserSession GetUserSession()
        {
            var session = new UserSession()
            {
                IP = HttpContext.Connection.RemoteIpAddress.ToString(),
                AccessToken = Guid.NewGuid().ToString(),
                LastLogin = DateTime.UtcNow.ToUnixTimestamp(),
                UserAgent = _detectionService.UserAgent.ToLower(),
                Type = _detectionService.Device.Type.ToString().ToLower(),
            };

            //if (!(_detectionService.Browser.Name == Browser.Unknown || _detectionService.Browser.Name == Browser.Others))
            //    session.UserAgent = _detectionService.Browser.Name.ToString().ToLower();

            return session;
        }
        #endregion

        #region Account Settings

        /// <param name="value" example="true" type="bolean"></param>
        /// <response code="400">enter value</response>
        /// <response code="400">value should be true or false</response>
        /// <response code="200">successfully changed</response>
        [HttpGet("AutoPlanRenew")]
        public async Task<IActionResult> AutoPlanRenew(string value = null)
        {
            if (value == null) return BadRequest("enter value");

            bool isValid = bool.TryParse(value, out bool val);
            if (!isValid)
                return BadRequest("value should be true or false");

            int id = this.GetAccountId();
            bool success = await _user.UpdatePlanAutoRenew(id, val);

            if (success)
                return Ok();
            else
                return BadRequest();
        }


        /// <param name="id">the sessionId</param>
        /// <response code="400">enter session id</response>
        /// <response code="400">wrong session id</response>
        /// <response code="200">successfully removed</response>
        [HttpGet("RemoveSession")]
        public async Task<IActionResult> RemoveSession(long? id = null)
        {
            bool success;
            int accountId = GetAccountId();
            string accountToken = GetAccountToken();
            if (id == null)
                success = await _user.RemoveSessionByAccountToken(accountId, accountToken);
            else
                success = await _user.RemoveSession(accountId, id.Value);

            if (success)
                return Ok();
            else
                return BadRequest("Wrong session id");
        }


        /// <response code="400">enter password</response>
        /// <response code="200">successfully changed</response>
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ReqChangePassword newPass)
        {
            if (string.IsNullOrWhiteSpace(newPass.Password)) return BadRequest("Enter password");
            int accountId = this.GetAccountId();

            bool success = await _user.UpdatePassword(accountId, newPass.Password);

            if (success)
                return Ok();
            else
                return BadRequest();
        }


        /// <response code="400">enter email</response>
        /// <response code="400">email exist</response>
        /// <response code="200">successfully changed</response>
        [HttpPost("ChangeEmail")]
        public async Task<IActionResult> ChangeEmail(ReqChangeEmail newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail.Email)) return BadRequest("Enter email");

            int accountId = this.GetAccountId();

            if (await _user.IsExistEmail(newEmail.Email))
                return BadRequest("email exist.");

            bool success = await _user.UpdateEmail(accountId, newEmail.Email);
            if (success)
                return Ok();
            else
                return BadRequest();
        }

        /// <response code="400">Enter userName/newUserName</response>
        /// <response code="400">userName exist</response>
        /// <response code="200">successfully changed</response>
        [HttpPost("ChangeUserName")]
        public async Task<IActionResult> ChangeUserName(ReqChangeUserName newUserName)
        {
            if (string.IsNullOrWhiteSpace(newUserName.UserName)) return BadRequest("Enter userName");

            int accountId = this.GetAccountId();

            if (await _user.IsExistUserName(newUserName.UserName))
                return BadRequest("userName exist.");

            bool success = await _user.UpdateUserName(accountId, newUserName.UserName);
            if (success)
                return Ok();
            else
                return BadRequest();
        }


        /// <response code="401">Unauthorized</response>
        /// <response code="202">Accepted</response>
        [HttpGet("Ping")]
        public async Task<IActionResult> Ping()
        {
            if (await _user.IsExistSession(GetAccountId(), GetAccountToken()))
                return Accepted();
            else
                return StatusCode((int)HttpStatusCode.Unauthorized, "");
        }

        [NonAction]
        private string GetAccountToken()
        {
            return Request.Headers["account-token"];
        }

        [NonAction]
        private int GetAccountId()
        {
            try
            {
                return int.Parse(Request.Headers["account-id"]);
            }
            catch
            {
                return 0;
            }
        }
        #endregion

        #region UserInfo
        /// <response code="404">wrong account-id (user not found)</response>
        /// <response code="200">returns user info</response>
        [HttpGet("UserInfo")]
        public async Task<IActionResult> UserInfo()
        {
            int accountId = GetAccountId();
            var userInfo = await _user.GetUserInfo(accountId);
            if (userInfo == default)
                return NotFound("wrong account-id (user not found)");
            else
                return Ok(userInfo);
        }


        /// <response code="404">wrong account-id (user not found)</response>
        /// <response code="200">returns the user sessions list</response>
        [HttpGet("SessionsList")]
        public async Task<IActionResult> SessionsList()
        {
            int accountId = GetAccountId();
            string accountToken = GetAccountToken();

            var sessions = await _user.GetSessions(accountId, accountToken);

            if (sessions == default)
                return NotFound("wrong account-id (user not found)");
            else
                return Ok(sessions);
        }
        #endregion
    }
}
