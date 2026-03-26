using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// Provides access to the system audit log.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Tags("Audit Logs")]
    [Produces("application/json")]
    public class AuditLogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Retrieves paginated audit log entries written by the logging pipeline.
        /// </summary>
        /// <param name="page">Page number (1-based, default 1).</param>
        /// <param name="pageSize">Number of entries per page (default 50).</param>
        /// <param name="search">Optional text to filter log messages.</param>
        /// <param name="sortBy">Column to sort by: date, status, or message (default date).</param>
        /// <param name="sortDirection">Sort direction: asc or desc (default desc).</param>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetAuditLogsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "date",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var result = await _mediator.Send(new GetAuditLogs
                {
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                });
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
