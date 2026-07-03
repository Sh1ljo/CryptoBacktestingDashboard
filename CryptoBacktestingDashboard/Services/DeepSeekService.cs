using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoBacktestingDashboard.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeepSeekService> _logger;
        private readonly AgentTools _agentTools;

        private const string DeepSeekApiUrl = "https://api.deepseek.com/chat/completions";

        // Default daily limit for normal users — overridable via config
        private int DailyLimit => _configuration.GetValue<int>("DeepSeek:DailyLimit", 50);

        public DeepSeekService(
            HttpClient httpClient,
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            ILogger<DeepSeekService> logger,
            AgentTools agentTools)
        {
            _httpClient = httpClient;
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _agentTools = agentTools;
        }

        /// <summary>
        /// Returns the number of assistant messages the user has sent today.
        /// </summary>
        public async Task<int> GetDailyUsageAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.AiChatLogs
                .CountAsync(l => l.UserId == userId && l.DateKey == today && l.Role == "assistant");
        }

        /// <summary>
        /// Returns how many assistant messages the user can still send today.
        /// Admin users always get -1 (unlimited).
        /// </summary>
        public async Task<int> GetRemainingAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return 0;

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin) return -1; // unlimited

            var used = await GetDailyUsageAsync(userId);
            return Math.Max(0, DailyLimit - used);
        }

        /// <summary>
        /// Check if the user can send a message.
        /// </summary>
        public async Task<(bool allowed, string? error)> CanSendMessageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
                return (true, null); // admin — always allowed

            var used = await GetDailyUsageAsync(userId);
            if (used >= DailyLimit)
                return (false, $"You've reached your daily limit of {DailyLimit} messages. It resets at midnight UTC.");

            return (true, null);
        }

        /// <summary>
        /// Send a chat message to DeepSeek and return the reply.
        /// </summary>
        public async Task<(string reply, string? error)> SendMessageAsync(
            string userId,
            string userMessage,
            List<ChatMessage> conversationHistory)
        {
            var apiKey = _configuration["DeepSeek:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return (string.Empty, "DeepSeek API key is not configured. Contact the administrator.");

            // Check rate limit
            var (allowed, error) = await CanSendMessageAsync(userId);
            if (!allowed)
                return (string.Empty, error);

            // Build system prompt with full app context
            var systemPrompt = BuildSystemPrompt();

            // Build messages array
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add conversation history (up to last 20 messages to avoid token overflow)
            foreach (var msg in conversationHistory.TakeLast(20))
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            // Add the new user message
            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = messages,
                temperature = 0.7,
                max_tokens = 2048,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, DeepSeekApiUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("DeepSeek API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    return (string.Empty, $"DeepSeek API error: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseBody);
                var reply = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

                if (string.IsNullOrEmpty(reply))
                    return (string.Empty, "Empty response from AI. Please try again.");

                // Log the exchange for daily counting
                var now = DateTime.UtcNow;
                _context.AiChatLogs.AddRange(
                    new AiChatLog
                    {
                        UserId = userId,
                        Role = "user",
                        Content = userMessage,
                        CreatedAt = now,
                        DateKey = now.Date
                    },
                    new AiChatLog
                    {
                        UserId = userId,
                        Role = "assistant",
                        Content = reply,
                        CreatedAt = now.AddMilliseconds(1),
                        DateKey = now.Date
                    }
                );
                await _context.SaveChangesAsync();

                return (reply, null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling DeepSeek API");
                return (string.Empty, "Network error: unable to reach DeepSeek API. Check your internet connection.");
            }
            catch (TaskCanceledException)
            {
                return (string.Empty, "Request timed out. Please try again.");
            }
        }

        /// <summary>
        /// Send a message with agentic capabilities — the AI can call tools to perform actions.
        /// </summary>
        public async Task<(string reply, List<ToolCallInfo>? toolCalls, string? error)> SendMessageAgenticAsync(
            string userId,
            string userMessage,
            List<ChatMessage> conversationHistory)
        {
            var apiKey = _configuration["DeepSeek:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return (string.Empty, null, "DeepSeek API key is not configured. Contact the administrator.");

            // Check rate limit
            var (allowed, error) = await CanSendMessageAsync(userId);
            if (!allowed)
                return (string.Empty, null, error);

            var executedTools = new List<ToolCallInfo>();
            var toolDefs = _agentTools.GetToolDefinitions();

            // ── Build messages ───────────────────────────────────────────

            var messages = new List<object>
            {
                new { role = "system", content = BuildAgentSystemPrompt() }
            };

            foreach (var msg in conversationHistory.TakeLast(20))
                messages.Add(new { role = msg.Role, content = msg.Content });

            messages.Add(new { role = "user", content = userMessage });

            // ── Build tools array for the API request ────────────────────

            var tools = toolDefs.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.InputSchema
                }
            }).ToList();

            // ── Function calling loop (max 10 rounds to prevent runaway) ─

            int maxRounds = 10;
            for (int round = 0; round < maxRounds; round++)
            {
                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = "deepseek-chat",
                    ["messages"] = messages,
                    ["tools"] = tools,
                    ["temperature"] = 0.3,   // lower temp = more deterministic tool choices
                    ["max_tokens"] = 4096,
                    ["stream"] = false
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, DeepSeekApiUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                try
                {
                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("DeepSeek API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                        return (string.Empty, executedTools, $"DeepSeek API error: {response.StatusCode}");
                    }

                    var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseBody);
                    if (result?.Choices == null || result.Choices.Count == 0)
                        return (string.Empty, executedTools, "Empty response from AI.");

                    var choice = result.Choices[0];
                    var message = choice.Message;

                    // ── Check for tool calls ─────────────────────────────
                    if (message?.ToolCalls != null && message.ToolCalls.Count > 0)
                    {
                        // Add the assistant's tool-call message to conversation
                        messages.Add(new
                        {
                            role = "assistant",
                            content = message.Content,
                            tool_calls = message.ToolCalls.Select(tc => new
                            {
                                id = tc.Id,
                                type = tc.Type,
                                function = new
                                {
                                    name = tc.Function.Name,
                                    arguments = tc.Function.Arguments
                                }
                            }).ToList()
                        });

                        // Execute each tool call
                        foreach (var tc in message.ToolCalls)
                        {
                            if (tc.Type != "function" || tc.Function == null) continue;

                            var toolName = tc.Function.Name;
                            var toolArgs = tc.Function.Arguments;

                            JsonElement argsElement;
                            try
                            {
                                argsElement = JsonSerializer.Deserialize<JsonElement>(toolArgs ?? "{}");
                            }
                            catch
                            {
                                argsElement = JsonSerializer.Deserialize<JsonElement>("{}");
                            }

                            _logger.LogInformation("Agent executing tool '{ToolName}' for user {UserId}", toolName, userId);

                            var (toolResult, success) = await _agentTools.ExecuteToolAsync(toolName, argsElement, userId);

                            executedTools.Add(new ToolCallInfo
                            {
                                Tool = toolName,
                                Args = toolArgs ?? "{}",
                                Result = toolResult,
                                Success = success
                            });

                            // Add the tool result message
                            messages.Add(new
                            {
                                role = "tool",
                                tool_call_id = tc.Id,
                                content = toolResult
                            });
                        }

                        // Continue loop to let AI process tool results
                        continue;
                    }

                    // ── No tool calls — this is the final text reply ─────
                    var reply = message?.Content?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(reply))
                        return (string.Empty, executedTools, "Empty response from AI. Please try again.");

                    // Log the exchange
                    var now = DateTime.UtcNow;
                    _context.AiChatLogs.AddRange(
                        new AiChatLog
                        {
                            UserId = userId,
                            Role = "user",
                            Content = userMessage,
                            CreatedAt = now,
                            DateKey = now.Date
                        },
                        new AiChatLog
                        {
                            UserId = userId,
                            Role = "assistant",
                            Content = reply,
                            CreatedAt = now.AddMilliseconds(1),
                            DateKey = now.Date
                        }
                    );
                    await _context.SaveChangesAsync();

                    return (reply, executedTools.Count > 0 ? executedTools : null, null);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error calling DeepSeek API");
                    return (string.Empty, executedTools, "Network error: unable to reach DeepSeek API.");
                }
                catch (TaskCanceledException)
                {
                    return (string.Empty, executedTools, "Request timed out. Please try again.");
                }
            }

            return (string.Empty, executedTools, "The AI took too many steps to complete your request. Please try a simpler request.");
        }

        /// <summary>
        /// Same agentic loop as <see cref="SendMessageAgenticAsync"/>, but instead of returning
        /// everything at the end it invokes <paramref name="emit"/> for each step as it happens:
        /// a <c>tool_start</c> when a tool begins, a <c>tool_end</c> when it finishes, and a
        /// final <c>reply</c> (or <c>error</c>). This lets the UI show progress step by step.
        /// </summary>
        public async Task SendMessageAgenticStreamAsync(
            string userId,
            string userMessage,
            List<ChatMessage> conversationHistory,
            Func<object, Task> emit,
            CancellationToken ct)
        {
            var apiKey = _configuration["DeepSeek:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                await emit(new { type = "error", error = "DeepSeek API key is not configured. Contact the administrator." });
                return;
            }

            var (allowed, error) = await CanSendMessageAsync(userId);
            if (!allowed)
            {
                await emit(new { type = "error", error });
                return;
            }

            var toolDefs = _agentTools.GetToolDefinitions();
            var executedCount = 0;

            var messages = new List<object>
            {
                new { role = "system", content = BuildAgentSystemPrompt() }
            };
            foreach (var msg in conversationHistory.TakeLast(20))
                messages.Add(new { role = msg.Role, content = msg.Content });
            messages.Add(new { role = "user", content = userMessage });

            var tools = toolDefs.Select(t => new
            {
                type = "function",
                function = new { name = t.Name, description = t.Description, parameters = t.InputSchema }
            }).ToList();

            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            int maxRounds = 10;
            for (int round = 0; round < maxRounds; round++)
            {
                ct.ThrowIfCancellationRequested();

                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = "deepseek-chat",
                    ["messages"] = messages,
                    ["tools"] = tools,
                    ["temperature"] = 0.3,
                    ["max_tokens"] = 4096,
                    ["stream"] = false
                };

                var json = JsonSerializer.Serialize(requestBody, jsonOpts);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, DeepSeekApiUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                HttpResponseMessage response;
                string responseBody;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, ct);
                    responseBody = await response.Content.ReadAsStringAsync(ct);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error calling DeepSeek API");
                    await emit(new { type = "error", error = "Network error: unable to reach DeepSeek API." });
                    return;
                }
                catch (TaskCanceledException)
                {
                    await emit(new { type = "error", error = "Request timed out. Please try again." });
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("DeepSeek API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    await emit(new { type = "error", error = $"DeepSeek API error: {response.StatusCode}" });
                    return;
                }

                var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseBody);
                var message = result?.Choices?.FirstOrDefault()?.Message;
                if (message == null)
                {
                    await emit(new { type = "error", error = "Empty response from AI." });
                    return;
                }

                // ── Tool calls this round ────────────────────────────────
                if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                {
                    messages.Add(new
                    {
                        role = "assistant",
                        content = message.Content,
                        tool_calls = message.ToolCalls.Select(tc => new
                        {
                            id = tc.Id,
                            type = tc.Type,
                            function = new { name = tc.Function!.Name, arguments = tc.Function.Arguments }
                        }).ToList()
                    });

                    foreach (var tc in message.ToolCalls)
                    {
                        if (tc.Type != "function" || tc.Function == null) continue;

                        var toolName = tc.Function.Name;
                        var toolArgs = tc.Function.Arguments;
                        var idx = executedCount++;

                        // Announce the step before running it so the UI shows a live spinner.
                        await emit(new { type = "tool_start", tool = toolName, index = idx });

                        JsonElement argsElement;
                        try { argsElement = JsonSerializer.Deserialize<JsonElement>(toolArgs ?? "{}"); }
                        catch { argsElement = JsonSerializer.Deserialize<JsonElement>("{}"); }

                        _logger.LogInformation("Agent executing tool '{ToolName}' for user {UserId}", toolName, userId);

                        var (toolResult, success) = await _agentTools.ExecuteToolAsync(toolName, argsElement, userId);

                        await emit(new { type = "tool_end", tool = toolName, index = idx, success });

                        messages.Add(new { role = "tool", tool_call_id = tc.Id, content = toolResult });
                    }

                    continue;
                }

                // ── Final text reply ─────────────────────────────────────
                var reply = message.Content?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(reply))
                {
                    await emit(new { type = "error", error = "Empty response from AI. Please try again." });
                    return;
                }

                var now = DateTime.UtcNow;
                _context.AiChatLogs.AddRange(
                    new AiChatLog { UserId = userId, Role = "user", Content = userMessage, CreatedAt = now, DateKey = now.Date },
                    new AiChatLog { UserId = userId, Role = "assistant", Content = reply, CreatedAt = now.AddMilliseconds(1), DateKey = now.Date });
                await _context.SaveChangesAsync(ct);

                await emit(new { type = "reply", reply });
                return;
            }

            await emit(new { type = "error", error = "The AI took too many steps to complete your request. Please try a simpler request." });
        }

        private string BuildSystemPrompt()
        {
            return BuildAgentSystemPrompt();
        }

        private string BuildAgentSystemPrompt()
        {
            return $@"You are an **agentic** AI trading assistant for the Crypto Backtesting Dashboard application. You have direct access to the application's database and can perform actions on the user's behalf using the tools available to you.

## Your Core Capabilities

1. **Answer questions** about trading concepts, indicators, and strategies.
2. **Query data** — list pairs, strategies, sessions, indicators, and view their details.
3. **Perform actions** — add pairs, create strategies, run backtests, and more.
4. **Guide users** through the application when they prefer to do things manually.

## How Tools Work

You have a set of tools/functions you can call. When a user asks you to do something:
- If you need data, **call the appropriate tool** immediately — don't tell the user to go to a page.
- If the user asks to create/modify something, **call the tool to do it**.
- After executing a tool, explain what happened in a clear, friendly way.
- You can call multiple tools in sequence if needed.

## Available Tools

- **list_pairs** / **get_pair** — Browse crypto pairs
- **add_pair** / **delete_pair** — Create or remove trading pairs
- **fetch_candles** — Download historical price data from Binance
- **list_strategies** / **get_strategy** — Browse strategies
- **create_strategy** / **delete_strategy** — Create or remove strategies
- **list_sessions** / **get_session** — Browse backtest sessions
- **create_session** — Create a new backtest session (links a strategy to a pair)
- **run_backtest** — Execute a backtest for a session
- **delete_session** — Remove a session
- **list_indicators** / **create_indicator** — Browse or create technical indicators

## Guidelines

- **Be proactive**: When a user asks ""Can you add ETH/USDT?"", just call add_pair and do it.
- **Be informative**: After performing an action, explain what was done and what the user can do next.
- **Be concise**: Use brief paragraphs, bullet points for lists, and bold for key numbers.
- **Confirm destructive actions**: Before deleting anything, briefly confirm with the user.
- **Explain failures**: If a tool call fails, explain why in plain language.
- **Stay on topic**: Focus on trading, backtesting, and the application features.
- **Use markdown**: Bold (**), lists (-), code blocks (```), and tables for structured data.
- **Risk reminder**: When discussing actual trading, remind users to never risk more than they can afford to lose.
";
        }

        // ── DTOs ──────────────────────────────────────────────────

        public class ChatMessage
        {
            public string Role { get; set; } = "user";
            public string Content { get; set; } = string.Empty;
        }

        private class DeepSeekResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("choices")]
            public List<DeepSeekChoice>? Choices { get; set; }

            [JsonPropertyName("usage")]
            public DeepSeekUsage? Usage { get; set; }
        }

        private class DeepSeekChoice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public DeepSeekMessage? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class DeepSeekMessage
        {
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }

            [JsonPropertyName("tool_calls")]
            public List<DeepSeekToolCall>? ToolCalls { get; set; }
        }

        private class DeepSeekToolCall
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = "function";

            [JsonPropertyName("function")]
            public DeepSeekFunctionCall? Function { get; set; }
        }

        private class DeepSeekFunctionCall
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("arguments")]
            public string? Arguments { get; set; }
        }

        private class DeepSeekUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}
