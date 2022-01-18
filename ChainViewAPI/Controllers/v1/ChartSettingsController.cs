using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserRepository;

namespace ChainViewAPI.Controllers.v1
{
    [Route("v1/api")]
    [ApiController]
    public class ChartSettingsController : Controller
    {
        private IUserRepository _userRepo;
        public ChartSettingsController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }


        /// <response code="200">Correct</response>
        /// <response code="400">(failed to edit)</response>
        /// <response code="400">the data in body is required</response>
        /// <response code="400">body is too big</response>
        [HttpPost("mdfycs")]
        public async Task<IActionResult> ModifyChartSettings()
        {
            string body;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest("the data in body is required");
                else if (body.Length > 1000)
                    return BadRequest("body is too big");
            }

            int userId = this.GetAccountId();
            bool correct = await _userRepo.EditChartSettings(userId, body);

            if (correct)
                return Ok("Correct");
            else
                return BadRequest("");
        }

        /// <response code="200">the settings</response>
        /// <response code="204">its empty</response>
        [HttpGet("cs")]
        public async Task<IActionResult> GetChartSettings()
        {
            int userId = this.GetAccountId();
            string chartSettings = await _userRepo.GetChartSettings(userId);
            return Ok(chartSettings);
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
