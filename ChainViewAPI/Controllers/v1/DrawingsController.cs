using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserRepository;

namespace ChainViewAPI.Controllers.v1
{
    [ApiController]
    [Route("v1/api")]
    public class DrawingsController : Controller
    {
        private ILayerRepository _layerRepo;
        private IUserRepository _userRepo;
        private IDrawingRepository _drawingRepo;
        public DrawingsController(ILayerRepository layerRepo, IUserRepository userRepo, IDrawingRepository drawingRepo)
        {
            _drawingRepo = drawingRepo;
            _layerRepo = layerRepo;
            _userRepo = userRepo;
        }

        /// <response code="400">Wrong 'layer'</response>
        /// <response code="200">the id of drawing</response>
        /// <response code="400">the data in body is required</response>
        [HttpPost("AddDraw")]
        public async Task<IActionResult> AddDraw(
            [Required] string exchange,
            [Required] string symbol,
            [Required] string layer,
            [Required] int type)
        {
            string body;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest("the data in body is required");
            }

            bool isDefaultLayer = layer.Equals("default");
            long layerId = 0;

            if (!isDefaultLayer)
                if (!long.TryParse(layer, out layerId))
                    return BadRequest("Wrong 'layer'");
            int userId = this.GetAccountId();
            long drawingId = await _drawingRepo.AddDrawingAsync(new UserModels.Drawing()
            {
                Data = body,
                LayerId = layerId,
                Type = type
            }, userId, layerId, exchange, symbol);


            if (drawingId == -1)
                return BadRequest("Wrong 'layer'");
            else
                return Ok(drawingId);
        }


        /// <response code="400">Wrong draw | there is no draw with this id or its not for this user</response>
        /// <response code="200">Correct</response>
        [HttpGet("DeleteDraw")]
        public async Task<IActionResult> DeleteDraw(
            [Required] long id)
        {
            int userId = this.GetAccountId();
            bool isDeleted = await _drawingRepo.DeleteDrawingAsync(userId, id);
            if (isDeleted)
            {
                return Ok("Correct");
            }
            else
            {
                return BadRequest("Wrong draw");
            }
        }

        /// <response code="400">Wrong draw | there is no draw with this id or its not for this user</response>
        /// <response code="200">Correct</response>
        /// <response code="400">the data in body is required</response>
        [HttpPost("ModifyDraw")]
        public async Task<IActionResult> ModifyDraw(
            [Required] long id)
        {
            string body;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest("the data in body is required");
            }

            int userId = this.GetAccountId();

            bool isModified = await _drawingRepo.ModifyDrawingAsync(userId, id, body);
            if (isModified)
            {
                return Ok("Correct");
            }
            else
            {
                return BadRequest("Wrong draw");
            }
        }


        /// <response code="400">Wrong 'layer'</response>
        /// <response code="200">Correct</response>
        [HttpGet("Drawings")]
        public async Task<IActionResult> GetAllDrawings(
            [Required] string exchange,
            [Required] string symbol,
            [Required] string layer)
        {
            bool isDefaultLayer = layer.Equals("default");
            long layerId = 0;

            if (!isDefaultLayer)
                if (!long.TryParse(layer, out layerId))
                    return BadRequest("Wrong 'layer'");

            int userId = this.GetAccountId();
            var layers = await _layerRepo.GetUserLayersAsync(userId, exchange, symbol);
            var drawings = await _drawingRepo.GetAllDrawingsAsync(userId, exchange, symbol, layerId);

            return Ok(new
            {
                layers,
                drawings
            });
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
