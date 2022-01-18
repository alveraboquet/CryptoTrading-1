using ChainViewAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseRepository;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Utilities;
using ExchangeServices;
using ChainViewAPI.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChainViewAPI.Controllers
{
    [Route("v1/api")]
    [ApiController]
    public class SymbolsController : ControllerBase
    {
        Stopwatch timer;
        private readonly IPairInfoRepository _pairRepo;
        private readonly IMemoryCache _cache;
        private readonly SymbolsStartAndEndTimeProvider getStart_End;
        public SymbolsController(IPairInfoRepository pairRepo, IMemoryCache cache, IBinanceServices api, SymbolsStartAndEndTimeProvider getStart_End)
        {
            this.getStart_End = getStart_End;
            _pairRepo = pairRepo;
            _cache = cache;
        }

        /// <param name="exchange" example="binance"></param>
        /// <param name="symbol" example="BTCUSDT">symbol name</param>
        /// <response code="404">no symbol found</response>
        /// <response code="200">returns the pair info</response>
        /// <response code="429">Too many request to binance.com</response>
        [HttpGet("SymbolInfo")]
        public async Task<IActionResult> SymbolInfo(
            [Required] string exchange,
            [Required] string symbol)
        {
            if (!_cache.TryGetPairInfo(exchange, symbol, out var pair))
            {
                return NotFound("No symbol found");
            }
            else
            {
                try
                {
                    var res = await pair.SymbolInfoResponseMessage(getStart_End, _pairRepo);
                    return Ok(res);
                }
                catch (BinanceTooManyRequestException ex)
                { return StatusCode(429, ex.Message); }
            }
        }

        /// <param name="exchange" example="binance"></param>
        /// <response code="200">returns the list of pair infos</response>
        [HttpGet("LS")]
        public async Task<IActionResult> ListOfSymbols(string exchange)
        {
            if (string.IsNullOrEmpty(exchange))
            {
                var pairs = _cache.TryGetPairInfoListResponse();
                return Ok(pairs);
            }
            else
            {
                var pairs = _cache.TryGetPairInfoList();
                var targetPairs = pairs.Where(p => p.Exchange == exchange).ToList().SymbolListResponseMessage();
                if (targetPairs.Length > 0) 
                {
                    return Ok(targetPairs);
                }
                else
                {
                    return NotFound("Doesn't exists.");
                }
            }
        }

        /// <param name="exchange" example="binance"></param>
        /// <param name="text" example="BTC">text to search</param>
        /// <response code="404">no symbol found</response>
        /// <response code="400">text is required</response>
        /// <response code="200">returns the list of pairs</response>
        [HttpGet("search")]
        public async Task<IActionResult> SymbolSearch([Required] string text, string exchange)
        {
            if (!string.IsNullOrEmpty(exchange))
            {
                try
                {
                    ApplicationValues.IsValidExchange(exchange);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("text is required.");

            text = text.ToUpper();


            try
            {
                if (_cache.TryGetSymbolSearch(exchange, text, out string val))
                    return Ok(val);
            }
            catch { }


            if (string.IsNullOrEmpty(exchange))
            {
                exchange = string.Empty;

                var pairs = _cache.TryGetPairInfoList();
                pairs = pairs.OrderBy(x => x.Symbol.StartsWith(text) ? 0 : 1).Take(20).ToList();

                _cache.SetSymbolSearch(exchange, text, pairs);

                if (pairs != null && pairs.Count > 0)
                    return Ok(pairs.SymbolSearchResponseMessage());
                else
                    return NotFound("Doesn't exists.");
            }
            else
            {
                var pairs = _cache.TryGetPairInfoList();
                pairs = pairs.OrderBy(x => x.Symbol.StartsWith(text) ? 0 : 1).Take(20).ToList();

                _cache.SetSymbolSearch(exchange, text, pairs);

                if (pairs != null && pairs.Count > 0)
                    return Ok(pairs.SymbolSearchResponseMessage());
                else
                    return NotFound("Doesn't exists.");
            }
        }
    }
}
