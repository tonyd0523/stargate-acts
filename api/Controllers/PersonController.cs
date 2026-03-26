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
    /// Manages people in the Astronaut Career Tracking System.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Tags("People")]
    [Produces("application/json")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly StargateContext _context;
        private readonly IWebHostEnvironment _env;
        public PersonController(IMediator mediator, StargateContext context, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Retrieves all people and their current astronaut details.
        /// </summary>
        /// <returns>A list of all people with astronaut information, if any.</returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetPeopleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPeople()
        {
            try
            {
                var result = await _mediator.Send(new GetPeople()
                {

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
        /// Retrieves a single person by name, including their current astronaut details.
        /// </summary>
        /// <param name="name">The person's full name (e.g. "Neil Armstrong").</param>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(GetPersonByNameResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName()
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
        /// Renames an existing person.
        /// </summary>
        /// <param name="name">The person's current name.</param>
        /// <param name="newName">The new name to assign.</param>
        [HttpPut("{name}")]
        [ProducesResponseType(typeof(UpdatePersonResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] string newName)
        {
            try
            {
                var result = await _mediator.Send(new UpdatePerson()
                {
                    Name = name,
                    NewName = newName
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
        /// Creates a new person. Names must be unique.
        /// </summary>
        /// <param name="name">The person's full name.</param>
        [HttpPost("")]
        [ProducesResponseType(typeof(CreatePersonResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePerson([FromBody] string name)
        {
            try
            {
                var result = await _mediator.Send(new CreatePerson()
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
        /// Uploads a photo for a person. Accepts JPEG or PNG up to 5 MB.
        /// </summary>
        /// <param name="name">The person's full name.</param>
        /// <param name="file">The image file (JPEG or PNG).</param>
        [HttpPost("{name}/photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadPhoto(string name, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return this.GetResponse(new BaseResponse { Message = "No file provided.", Success = false, ResponseCode = (int)HttpStatusCode.BadRequest });

                if (file.Length > 5 * 1024 * 1024)
                    return this.GetResponse(new BaseResponse { Message = "File exceeds 5 MB limit.", Success = false, ResponseCode = (int)HttpStatusCode.BadRequest });

                // ContentType is client-supplied and cannot be trusted as proof of file format.
                // In production, inspect the file's magic bytes (e.g. using FileSignatures or
                // similar) rather than relying on the MIME header alone to prevent disguised uploads.
                var allowedTypes = new[] { "image/jpeg", "image/png" };
                if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                    return this.GetResponse(new BaseResponse { Message = "Only JPEG and PNG files are allowed.", Success = false, ResponseCode = (int)HttpStatusCode.BadRequest });

                var person = await _context.People.FirstOrDefaultAsync(p => p.Name == name);
                if (person == null)
                    return this.GetResponse(new BaseResponse { Message = $"Person '{name}' not found.", Success = false, ResponseCode = (int)HttpStatusCode.NotFound });

                var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
                // Minimal sanitization: lowercases and replaces spaces, but does not handle
                // characters like '..', '/', or '\' that could produce a path traversal.
                // In production, use a GUID-based filename to eliminate the risk entirely.
                var safeName = name.ToLowerInvariant().Replace(" ", "-");
                var fileName = $"{safeName}{extension}";

                // Stored under wwwroot/photos/ so ASP.NET Core's static file middleware
                // serves them directly without a controller round-trip.
                var photosDir = Path.Combine(_env.WebRootPath, "photos");
                Directory.CreateDirectory(photosDir);

                var filePath = Path.Combine(photosDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                person.PhotoUrl = $"photos/{fileName}";
                await _context.SaveChangesAsync();

                return this.GetResponse(new BaseResponse { Message = "Photo uploaded successfully.", Success = true, ResponseCode = (int)HttpStatusCode.OK });
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

        /// <summary>
        /// Deletes a person and all their associated astronaut details, duties, and photo.
        /// </summary>
        /// <param name="name">The person's full name.</param>
        [HttpDelete("{name}")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePerson(string name)
        {
            try
            {
                var person = await _context.People.FirstOrDefaultAsync(p => p.Name == name);
                if (person == null)
                    return this.GetResponse(new BaseResponse { Message = $"Person '{name}' not found.", Success = false, ResponseCode = (int)HttpStatusCode.NotFound });

                // Explicit cascade: manually delete duties and detail before the person.
                // EF Core could handle this automatically via DeleteBehavior.Cascade in
                // PersonConfiguration, but explicit deletion makes the order and scope visible
                // and avoids surprises if the cascade configuration ever changes.
                var duties = _context.AstronautDuties.Where(d => d.PersonId == person.Id);
                _context.AstronautDuties.RemoveRange(duties);

                var detail = await _context.AstronautDetails.FirstOrDefaultAsync(d => d.PersonId == person.Id);
                if (detail != null) _context.AstronautDetails.Remove(detail);

                // Delete photo file if exists
                if (!string.IsNullOrEmpty(person.PhotoUrl))
                {
                    var photoPath = Path.Combine(_env.WebRootPath, person.PhotoUrl);
                    if (System.IO.File.Exists(photoPath)) System.IO.File.Delete(photoPath);
                }

                _context.People.Remove(person);
                await _context.SaveChangesAsync();

                return this.GetResponse(new BaseResponse { Message = $"'{name}' deleted.", Success = true, ResponseCode = (int)HttpStatusCode.OK });
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