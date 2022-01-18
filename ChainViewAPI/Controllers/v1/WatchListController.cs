using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using UserRepository;

namespace ChainViewAPI.Controllers.v1
{
    [Route("v1/api")]
    [ApiController]
    public class WatchListController : Controller
    {
        private readonly IUserRepository _user;
        public WatchListController(IUserRepository user)
        {
            _user = user;
        }

        /// <response code="200"></response>
        [HttpGet("AddWL")]
        public async Task<IActionResult> Add(
            [Required] string exchange,
            [Required] string symbol)
        {
            var userId = GetAccountId();

            return Ok(await _user.AddWatchList(userId, exchange, symbol));
        }

        /// <response code="200"></response>
        [HttpGet("rmWL")]
        public async Task<IActionResult> Remove(
            [Required] string exchange,
            [Required] string symbol)
        {
            var userId = GetAccountId();

            return Ok(await _user.RemoveWatchList(userId, exchange, symbol));
        }


        /// <response code="200"></response>
        [HttpGet("wl")]
        public async Task<IActionResult> Get()
        {
            var userId = GetAccountId();
            return Ok(await _user.GetWatchList(userId));
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
    }
}
