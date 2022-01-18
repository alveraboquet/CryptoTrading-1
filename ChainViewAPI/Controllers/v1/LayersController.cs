using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UserModels;
using UserRepository;

namespace ChainViewAPI.Controllers
{
    [Route("v1/api")]
    [ApiController]
    public class LayersController : ControllerBase
    {
        Stopwatch timer;
        private ILayerRepository _layerRepo;
        private IUserRepository _userRepo;

        public LayersController(ILayerRepository layerRepo, IUserRepository userRepo)
        {
            timer = new Stopwatch();
            _layerRepo = layerRepo;
            _userRepo = userRepo;
        }


        /// <response code="403">max layers 'returns max layers for this user'</response>
        /// <response code="200">the id and name of layer</response>
        [HttpGet("AddLayer")]
        public async Task<IActionResult> AddLayer(
            [Required] string exchange,
            [Required] string symbol, 
            [Required] string name)
        {
            int userId = GetAccountId();
            int userPlan = await _userRepo.GetPlanAsync(userId);
            int layersCount = await _layerRepo.GetLayersCountAsync(userId, exchange, symbol);
            switch (userPlan)
            {
                default:
                case 0:
                    if (layersCount >= 2)
                        return StatusCode(403, "2");
                    break;
                case 1:
                    if (layersCount >= 5)
                        return StatusCode(403, "5");
                    break;
                case 2:
                    if (layersCount >= 10)
                        return StatusCode(403, "10");
                    break;
            }

            Layer layer = new Layer()
            {
                Exchange = exchange,
                Symbol = symbol,
                IsDefault = (layersCount <= 0),
                Name = name,
                UserId = userId
            };

            await _layerRepo.AddLayerAsync(layer);
            return Ok(new
            {
                layer.Id,
                layer.Name
            });
        }


        /// <response code="400">Can not set as default</response>
        /// <response code="200">the list of layers</response>
        [HttpGet("SetDefLayer")]
        public async Task<IActionResult> SetLayerAsDefault(
            [Required] string exchange,
            [Required] string symbol,
            [Required] long id)
        {
            int userId = GetAccountId();

            if (await _layerRepo.SetLayerAsDefualtAsync(userId, exchange, symbol, id))
                return Ok(await _layerRepo.GetUserLayersAsync(userId, exchange, symbol));
            else
                return BadRequest("Can not set as default");
        }


        /// <response code="400">wrong layer id.</response>
        /// <response code="200">ther list of layers</response>
        [HttpGet("DeleteLayer")]
        public async Task<IActionResult> DeleteLayer(
            [Required] string exchange,
            [Required] string symbol,
            [Required] long id)
        {
            int userId = GetAccountId();
            try
            {
                var res = await _layerRepo.DeleteLayerAndHandleAsync(userId, exchange, symbol, id);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [NonAction]
        private string GetAccountToken() => Request.Headers["account-token"];
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
