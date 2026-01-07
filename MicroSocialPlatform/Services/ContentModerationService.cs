using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace MicroSocialPlatform.Services
{
    /// <summary>
    /// Serviciu de moderare a conținutului folosind Claude AI (Anthropic)
    /// Analizează textul pentru limbaj nepotrivit, insulte, hate speech, etc.
    /// </summary>
    public class ContentModerationService : IContentModerationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ContentModerationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl = "https://api.anthropic.com/v1/messages";

        public ContentModerationService(
            IHttpClientFactory httpClientFactory,
            ILogger<ContentModerationService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Moderează conținutul folosind Claude AI
        /// </summary>
        public async Task<ModerationResult> ModerateContentAsync(string text)
        {
            Console.WriteLine("Apelul pentru moderarea de continut este facut");
            Console.WriteLine($"textul de moderat este: {text}");

            // Validare input
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ModerationResult
                {
                    IsClean = true,
                    Confidence = 1.0
                };
            }

            try
            {
                _logger.LogInformation("Moderating content with AI...");
                Console.WriteLine("Construim prompt ul....");

                // Construiește prompt-ul pentru Claude
                var prompt = BuildModerationPrompt(text);
                Console.WriteLine($"Promptul este construit: {prompt.Substring(0, 100)}...");

                // Apelează Claude API
                Console.WriteLine("Calling Claude API...");
                var response = await CallClaudeApiAsync(prompt);
                Console.WriteLine($"API Response received: {response.Substring(0, 200)}...");

                // Parsează răspunsul
                var result = ParseModerationResponse(response);
                Console.WriteLine($"Parsed result - IsClean: {result.IsClean}, Reason: {result.Reason}");

                _logger.LogInformation($"Moderation result: IsClean={result.IsClean}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in moderation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error during content moderation");

                // În caz de eroare, permitem conținutul (fail-safe)
                // SAU poți alege să blochezi totul în caz de eroare
                return new ModerationResult
                {
                    IsClean = true,
                    Reason = "Verificare AI indisponibilă",
                    Confidence = 0.0
                };
            }
        }

        /// <summary>
        /// Construiește prompt-ul pentru analiza de moderare
        /// </summary>
        private string BuildModerationPrompt(string text)
        {
            return $@"Analizează următorul text în limba română și determină dacă conține conținut neadecvat.
            Conținut de analizat: ""{text}""
            Categorizează textul după aceste criterii:
            1. Insulte și limbaj vulgar
            2. Hate speech (discurs de ură bazat pe rasă, etnie, religie, gen, orientare sexuală)
            3. Amenințări sau incitare la violență
            4. Limbaj discriminatoriu
            5. Hărțuire sau bullying
            6. Conținut sexual explicit nepotrivit

            Răspunde DOAR cu un obiect JSON în următorul format (fără alt text):
            {{
                ""isClean"": true/false,
                ""reason"": ""scurtă explicație în română (maxim 100 caractere)"",
                ""detectedIssues"": [""categorie1"", ""categorie2""],
                ""confidence"": 0.0-1.0
            }}

            Dacă textul este curat și acceptabil, setează isClean=true.
            Dacă textul conține conținut neadecvat, setează isClean=false și explică motivul.
            Fii rezonabil și ține cont de context - nu bloca limbaj normal sau expresii comune.";
        }

        /// <summary>
        /// Apelează Claude API pentru analiza textului
        /// </summary>
        private async Task<string> CallClaudeApiAsync(string prompt)
        {
            Console.WriteLine("=== CALLING CLAUDE API ===");
            var client = _httpClientFactory.CreateClient();

            // citesc API key din configuratie
            var apiKey = _configuration["AnthropicApi:ApiKey"];
            Console.WriteLine($"API Key loaded: {(string.IsNullOrEmpty(apiKey) ? "NO" : "YES")}");
            Console.WriteLine($"API Key starts with: {(apiKey?.Substring(0, 10) ?? "NULL")}...");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("API Key pentru Anthropic nu este configurat!");
            }

            // headerele necesare pt Anthropic API
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey); 
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = "claude-sonnet-4-20250514",
                max_tokens = 500,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            Console.WriteLine($"Request JSON: {jsonContent.Substring(0, 200)}...");
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending POST to: {_apiUrl}");
            var response = await client.PostAsync(_apiUrl, content);

            Console.WriteLine($"Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ERROR Response: {errorBody}");
                throw new HttpRequestException($"API call failed: {response.StatusCode} - {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Success! Response length: {responseBody.Length}");

            return responseBody;
        }

        /// <summary>
        /// Parsează răspunsul de la Claude și extrage rezultatul moderării
        /// </summary>
        private ModerationResult ParseModerationResponse(string apiResponse)
        {
            try
            {
                // Parse JSON response from Claude
                using var doc = JsonDocument.Parse(apiResponse);
                var root = doc.RootElement;

                // Extrage textul din răspunsul Claude
                var contentArray = root.GetProperty("content");
                var textContent = contentArray[0].GetProperty("text").GetString() ?? "";

                // Curăță textul de markdown code blocks dacă există
                textContent = textContent.Trim();
                if (textContent.StartsWith("```json"))
                {
                    textContent = textContent.Substring(7);
                }
                if (textContent.StartsWith("```"))
                {
                    textContent = textContent.Substring(3);
                }
                if (textContent.EndsWith("```"))
                {
                    textContent = textContent.Substring(0, textContent.Length - 3);
                }
                textContent = textContent.Trim();

                // Parse rezultatul de moderare
                using var resultDoc = JsonDocument.Parse(textContent);
                var resultRoot = resultDoc.RootElement;

                var isClean = resultRoot.GetProperty("isClean").GetBoolean();
                var reason = resultRoot.TryGetProperty("reason", out var reasonProp)
                    ? reasonProp.GetString()
                    : null;
                var confidence = resultRoot.TryGetProperty("confidence", out var confProp)
                    ? confProp.GetDouble()
                    : 0.8;

                var detectedIssues = new List<string>();
                if (resultRoot.TryGetProperty("detectedIssues", out var issuesProp))
                {
                    foreach (var issue in issuesProp.EnumerateArray())
                    {
                        detectedIssues.Add(issue.GetString() ?? "");
                    }
                }

                return new ModerationResult
                {
                    IsClean = isClean,
                    Reason = reason,
                    DetectedIssues = detectedIssues,
                    Confidence = confidence
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing moderation response");

                // În caz de eroare de parsare, blochează conținutul pentru siguranță
                return new ModerationResult
                {
                    IsClean = false,
                    Reason = "Nu am putut verifica conținutul. Te rugăm să reformulezi.",
                    Confidence = 0.0
                };
            }
        }
    }
}
