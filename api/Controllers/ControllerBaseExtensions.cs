using Microsoft.AspNetCore.Mvc;

namespace StargateAPI.Controllers
{
    // Maps a BaseResponse to an IActionResult where the HTTP status code is driven by
    // ResponseCode embedded in the response body. Using an extension method keeps this
    // reusable across all controllers without forcing a shared base class. The HTTP
    // status and the body's ResponseCode are always in sync by construction.
    public static class ControllerBaseExtensions
    {
        public static IActionResult GetResponse(this ControllerBase controllerBase, BaseResponse response)
        {
            var httpResponse = new ObjectResult(response);
            httpResponse.StatusCode = response.ResponseCode;
            return httpResponse;
        }
    }
}