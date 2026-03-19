using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatGptController : ControllerBase
    {
        private readonly ILogger<ChatGptController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatGptController(ILogger<ChatGptController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Test endpoint to verify the controller is working
        /// </summary>
        [HttpGet("test")]
        public ActionResult<object> Test()
        {
            var geminiApiKey = _configuration["GeminiApiKey"] ?? _configuration["Gemini:ApiKey"];
            var openAiApiKey = _configuration["OpenAIApiKey"] ?? _configuration["OpenAI:ApiKey"];
            var hasGeminiKey = !string.IsNullOrEmpty(geminiApiKey) && geminiApiKey != "YOUR_GEMINI_API_KEY_HERE";
            var hasOpenAiKey = !string.IsNullOrEmpty(openAiApiKey) && openAiApiKey != "YOUR_OPENAI_API_KEY_HERE";
            
            return Ok(new { 
                message = "AI Chat controller is working", 
                geminiApiKeyConfigured = hasGeminiKey,
                openAiApiKeyConfigured = hasOpenAiKey,
                recommendedApi = hasGeminiKey ? "Gemini (Free)" : hasOpenAiKey ? "OpenAI (Paid)" : "None"
            });
        }

        /// <summary>
        /// Send a prompt to AI (Gemini or ChatGPT) and get response
        /// </summary>
        [HttpPost("chat")]
        public async Task<ActionResult<object>> Chat([FromBody] ChatGptRequest? request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Received null request body");
                    return BadRequest(new { message = "Request body cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    _logger.LogWarning("Received empty prompt");
                    return BadRequest(new { message = "Prompt cannot be empty" });
                }

                // Try Gemini API first (free tier), fallback to OpenAI if configured
                var geminiApiKey = _configuration["GeminiApiKey"] ?? _configuration["Gemini:ApiKey"];
                var openAiApiKey = _configuration["OpenAIApiKey"] ?? _configuration["OpenAI:ApiKey"];
                
                if (!string.IsNullOrEmpty(geminiApiKey) && geminiApiKey != "YOUR_GEMINI_API_KEY_HERE")
                {
                    return await CallGeminiApi(request.Prompt, geminiApiKey);
                }
                else if (!string.IsNullOrEmpty(openAiApiKey) && openAiApiKey != "YOUR_OPENAI_API_KEY_HERE")
                {
                    return await CallOpenAiApi(request.Prompt, openAiApiKey);
                }
                else
                {
                    _logger.LogWarning("No AI API key configured");
                    return StatusCode(500, new { 
                        message = "No AI API key configured. Please set 'GeminiApiKey' in appsettings.json. Get a FREE API key from https://aistudio.google.com/app/apikey" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI API");
                return StatusCode(500, new { message = "An error occurred while calling AI API", error = ex.Message });
            }
        }

        /// <summary>
        /// Call Google Gemini API (FREE tier available)
        /// </summary>
        private async Task<ActionResult<object>> CallGeminiApi(string prompt, string apiKey)
        {
            try
            {
                // Google Gemini API endpoint (free tier: 60 requests/minute)
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

                // Prepare request body for Gemini
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1000
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to Gemini API with prompt length: {Length}", prompt.Length);

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        string? errorMessage = null;
                        
                        if (errorObj.TryGetProperty("error", out var errorProp))
                        {
                            if (errorProp.TryGetProperty("message", out var msgProp))
                            {
                                errorMessage = msgProp.GetString();
                            }
                            else
                            {
                                errorMessage = errorProp.GetRawText();
                            }
                        }
                        
                        var userMessage = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                            ? "Rate limit exceeded. Gemini free tier allows 60 requests per minute. Please wait a moment and try again."
                            : $"Error calling Gemini API: {errorMessage ?? responseContent}";
                        
                        return StatusCode((int)response.StatusCode, new { message = userMessage, error = errorMessage ?? responseContent });
                    }
                    catch
                    {
                        return StatusCode((int)response.StatusCode, new { message = "Error calling Gemini API", error = responseContent });
                    }
                }

                // Parse Gemini response
                _logger.LogDebug("Gemini API response: {Response}", responseContent);
                
                var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
                {
                    _logger.LogWarning("No candidates in Gemini response. Full response: {Response}", responseContent);
                    return Ok(new { response = "No response from Gemini", rawResponse = responseContent });
                }

                var textResponse = geminiResponse.Candidates[0].Content?.Parts?[0]?.Text ?? "No text response";
                
                if (textResponse == "No text response")
                {
                    _logger.LogWarning("No text in Gemini response. Full response: {Response}", responseContent);
                }

                _logger.LogInformation("Received response from Gemini API, length: {Length}", textResponse.Length);

                return Ok(new { response = textResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return StatusCode(500, new { message = "An error occurred while calling Gemini API", error = ex.Message });
            }
        }

        /// <summary>
        /// Call OpenAI ChatGPT API (fallback if Gemini not configured)
        /// </summary>
        private async Task<ActionResult<object>> CallOpenAiApi(string prompt, string apiKey)
        {
            // OpenAI ChatGPT API endpoint
            var apiUrl = "https://api.openai.com/v1/chat/completions";

            // Prepare request body for OpenAI
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Create a new HttpClient for this request with the API key
            using var requestClient = new HttpClient();
            requestClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            _logger.LogInformation("Sending request to ChatGPT API with prompt length: {Length}", prompt.Length);

            var response = await requestClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ChatGPT API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    string? errorMessage = null;
                    
                    if (errorObj.TryGetProperty("error", out var errorProp))
                    {
                        if (errorProp.TryGetProperty("message", out var msgProp))
                        {
                            errorMessage = msgProp.GetString();
                        }
                        else
                        {
                            errorMessage = errorProp.GetRawText();
                        }
                    }
                    
                    var userMessage = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                                    (errorMessage?.Contains("quota", StringComparison.OrdinalIgnoreCase) == true)
                        ? "Your OpenAI account has exceeded its quota or needs billing setup. Please check your plan and billing details at https://platform.openai.com/account/billing"
                        : $"Error calling ChatGPT API: {errorMessage ?? responseContent}";
                    
                    return StatusCode((int)response.StatusCode, new { message = userMessage, error = errorMessage ?? responseContent });
                }
                catch
                {
                    return StatusCode((int)response.StatusCode, new { message = "Error calling ChatGPT API", error = responseContent });
                }
            }

            // Parse OpenAI response
            _logger.LogDebug("ChatGPT API response: {Response}", responseContent);
            
            var openAiResponse = JsonSerializer.Deserialize<OpenAiApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
            {
                _logger.LogWarning("No choices in ChatGPT response. Full response: {Response}", responseContent);
                return Ok(new { response = "No response from ChatGPT", rawResponse = responseContent });
            }

            var textResponse = openAiResponse.Choices[0].Message?.Content ?? "No text response";
            
            if (textResponse == "No text response")
            {
                _logger.LogWarning("No text in ChatGPT response. Full response: {Response}", responseContent);
            }

            _logger.LogInformation("Received response from ChatGPT API, length: {Length}", textResponse.Length);

            return Ok(new { response = textResponse });
        }
    }

    public class ChatGptRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    public class OpenAiApiResponse
    {
        [JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; set; }
    }

    public class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    public class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    // Gemini API Response Models
    public class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[]? Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

