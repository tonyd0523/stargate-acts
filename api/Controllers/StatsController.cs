using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// Provides aggregate statistics about people and astronaut duties.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Tags("Statistics")]
    [Produces("application/json")]
    public class StatsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StatsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns counts of total people, active astronauts, retired astronauts, and total duties.
        /// </summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetStatsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var result = await _mediator.Send(new GetStats());
                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }
    }
}
