using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class AiChatApiController : ControllerBase
    {
        private readonly DeepSeekService _deepSeekService;

        public AiChatApiController(DeepSeekService deepSeekService)
        {
            _deepSeekService = deepSeekService;
        }

        /// <summary>
        /// Send a message to the AI assistant and get a reply.
        /// The AI has agentic capabilities — it can call tools to perform actions.
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<object>> SendMessage([FromBody] ChatRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            // Truncate message length
            if (request.Message.Length > 4000)
                return BadRequest(new { error = "Message too long (max 4000 characters)." });

            var (reply, toolCalls, error) = await _deepSeekService.SendMessageAgenticAsync(
                userId,
                request.Message.Trim(),
                request.History ?? new List<DeepSeekService.ChatMessage>());

            if (error != null)
                return BadRequest(new { error });

            return Ok(new { reply, toolCalls });
        }

        /// <summary>
        /// Streaming variant of <see cref="SendMessage"/>. Emits Server-Sent Events so the UI
        /// can show each step live: a "tool_start"/"tool_end" event per tool as it runs, then a
        /// final "reply" (or "error"), and a closing "done".
        /// </summary>
        [HttpPost("stream")]
        public async Task Stream([FromBody] ChatRequest request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no"; // disable proxy buffering

            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            async Task Emit(object evt)
            {
                var json = JsonSerializer.Serialize(evt, jsonOpts);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                await Emit(new { type = "error", error = "User not authenticated." });
                await Emit(new { type = "done" });
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await Emit(new { type = "error", error = "Message cannot be empty." });
                await Emit(new { type = "done" });
                return;
            }

            if (request.Message.Length > 4000)
            {
                await Emit(new { type = "error", error = "Message too long (max 4000 characters)." });
                await Emit(new { type = "done" });
                return;
            }

            try
            {
                await _deepSeekService.SendMessageAgenticStreamAsync(
                    userId,
                    request.Message.Trim(),
                    request.History ?? new List<DeepSeekService.ChatMessage>(),
                    Emit,
                    HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected — nothing more to send.
                return;
            }

            await Emit(new { type = "done" });
        }

        /// <summary>
        /// Get the user's remaining daily message count.
        /// Returns -1 for admin (unlimited).
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated." });

            var remaining = await _deepSeekService.GetRemainingAsync(userId);
            var isAdmin = User.IsInRole("Admin");

            return Ok(new
            {
                remaining,
                isAdmin,
                unlimited = remaining == -1
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<DeepSeekService.ChatMessage>? History { get; set; }
    }
}
