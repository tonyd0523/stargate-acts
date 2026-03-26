using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// Manages astronaut duty assignments and career history.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Tags("Astronaut Duties")]
    [Produces("application/json")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly StargateContext _context;
        public AstronautDutyController(IMediator mediator, StargateContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        /// <summary>
        /// Retrieves all astronaut duties across all people, ordered by start date descending.
        /// </summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetAllAstronautDutiesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAstronautDuties()
        {
            try
            {
                var result = await _mediator.Send(new GetAllAstronautDuties());
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

        /// <summary>
        /// Retrieves a person's astronaut details and their complete duty history.
        /// </summary>
        /// <param name="name">The person's full name (e.g. "Sally Ride").</param>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(GetAstronautDutiesByNameResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAstronautDutiesByName(string name)
        {
            try
            {
                // FIX: The original sent GetPersonByName here instead of
                // GetAstronautDutiesByName, so this endpoint always returned
                // person data with no duty history.
                var result = await _mediator.Send(new GetAstronautDutiesByName()
                {
                    Name = name
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

        /// <summary>
        /// Creates a new astronaut duty assignment. Automatically updates the person's
        /// current astronaut detail and closes their previous duty.
        /// </summary>
        /// <param name="request">The duty assignment details.</param>
        [HttpPost("")]
        [ProducesResponseType(typeof(CreateAstronautDutyResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            try
            {
                var result = await _mediator.Send(request);
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

        /// <summary>
        /// Updates an existing astronaut duty by ID. If the duty is the person's current
        /// (open-ended) assignment, their astronaut detail is also updated.
        /// </summary>
        /// <param name="id">The duty record ID.</param>
        /// <param name="request">The updated duty fields.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UpdateAstronautDutyResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAstronautDuty(int id, [FromBody] UpdateAstronautDuty request)
        {
            try
            {
                request.Id = id;
                var result = await _mediator.Send(request);
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

        /// <summary>
        /// Deletes an astronaut duty by ID. If it was the person's current duty,
        /// their astronaut detail is updated to reflect the previous duty.
        /// </summary>
        /// <param name="id">The duty record ID.</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAstronautDuty(int id)
        {
            try
            {
                // Design note: the deletion logic and AstronautDetail sync below belong
                // in a MediatR command handler to be consistent with the CQRS pattern used
                // everywhere else. Moving it would also allow both SaveChangesAsync calls to
                // share a single unit of work, eliminating the window where the duty is
                // deleted but the detail snapshot is stale if the second save fails.
                var duty = await _context.AstronautDuties.FirstOrDefaultAsync(d => d.Id == id);
                if (duty == null)
                    return this.GetResponse(new BaseResponse { Message = $"Duty {id} not found.", Success = false, ResponseCode = (int)HttpStatusCode.NotFound });

                var wasCurrent = duty.DutyEndDate == null;
                var personId = duty.PersonId;

                _context.AstronautDuties.Remove(duty);
                await _context.SaveChangesAsync();

                // If the deleted duty was the current (open-ended) one, roll AstronautDetail
                // back to the previous duty. If no previous duty exists, remove the detail row
                // entirely — the person has no astronaut history left.
                if (wasCurrent)
                {
                    var detail = await _context.AstronautDetails.FirstOrDefaultAsync(d => d.PersonId == personId);
                    var previousDuty = await _context.AstronautDuties
                        .Where(d => d.PersonId == personId)
                        .OrderByDescending(d => d.DutyStartDate)
                        .FirstOrDefaultAsync();

                    if (detail != null)
                    {
                        if (previousDuty != null)
                        {
                            previousDuty.DutyEndDate = null; // reopen it
                            detail.CurrentRank = previousDuty.Rank;
                            detail.CurrentDutyTitle = previousDuty.DutyTitle;
                            detail.CareerEndDate = null;
                        }
                        else
                        {
                            // No duties left — remove detail
                            _context.AstronautDetails.Remove(detail);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                return this.GetResponse(new BaseResponse { Message = "Duty deleted.", Success = true, ResponseCode = (int)HttpStatusCode.OK });
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }
    }
}
