using System.Net;

namespace StargateAPI.Controllers
{
    // Response envelope used by every endpoint. ResponseCode mirrors the HTTP status code
    // so consumers can inspect the outcome from the JSON body alone without inspecting
    // HTTP headers — useful for clients that wrap all responses in a uniform error handler.
    // ControllerBaseExtensions.GetResponse() ensures the HTTP status and ResponseCode
    // are always set from the same value, keeping them in sync by construction.
    /// <summary>
    /// Standard envelope returned by every API endpoint.
    /// </summary>
    public class BaseResponse
    {
        /// <summary>Whether the request completed successfully.</summary>
        /// <example>true</example>
        public bool Success { get; set; } = true;

        /// <summary>Human-readable status or error message.</summary>
        /// <example>Successful</example>
        public string Message { get; set; } = "Successful";

        /// <summary>HTTP status code echoed in the response body.</summary>
        /// <example>200</example>
        public int ResponseCode { get; set; } = (int)HttpStatusCode.OK;
    }
}