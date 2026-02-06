using AiApi.Services.Gemini;
using AiApi.ViewModels;
using System.Text.Json;

namespace AiApi.Services
{
    public class GeminiAiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const string GeminiModel = "gemini-2.5-flash-preview-09-2025";

        public GeminiAiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<List<string>> SuggestTagsAsync(string content)
        {
            List<TagDto> existingTags;
            List<string> existingTagNamesList;

            // --- BƯỚC 1: LẤY TAGS HIỆN CÓ TỪ CORE API ---
            try
            {
                existingTags = await GetExistingTagsAsync();
                existingTagNamesList = existingTags?
                                    .Select(t => t.TagName?.Trim())
                                    .Where(name => !string.IsNullOrEmpty(name))
                                    .Distinct(StringComparer.OrdinalIgnoreCase) // Đảm bảo không trùng lặp
                                    .ToList()
                                    ?? new List<string>();

                if (!existingTagNamesList.Any())
                {
                    // Ghi log lỗi nghiêm trọng hơn nếu cần
                    return new List<string>(); // Không thể gợi ý nếu không có tag nào
                }
            }
            catch (Exception ex)
            {
                return new List<string>(); // Trả về rỗng nếu lỗi lấy tag
            }
            // ---------------------------------------------


            var apiKey = "AIzaSyBeUiYWE25ZV68hS0iRR5s1XkBCBvXJ-Xs"; 
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:generateContent?key={apiKey}";

            var systemPrompt = "Bạn là một chuyên gia biên tập tin tức. Nhiệm vụ của bạn là đọc nội dung bài viết và **chọn ra tối đa 5 thẻ (tags) phù hợp nhất từ danh sách thẻ có sẵn được cung cấp**. " +
                               "Các thẻ được chọn phải liên quan trực tiếp đến nội dung chính của bài viết. " +
                               "Trả về kết quả dưới dạng một đối tượng JSON có cấu trúc: {\"tags\": [\"thẻ được chọn 1\", \"thẻ được chọn 2\"]}. Chỉ trả về các thẻ có trong danh sách được cung cấp.";

            var availableTagsString = string.Join(", ", existingTagNamesList.Select(t => $"\"{t}\""));
            var userPrompt = $"Dưới đây là nội dung bài viết:\n\n{content}\n\n" +
                             $"Và đây là danh sách các thẻ hiện có:\n[{availableTagsString}]\n\n" +
                             "Hãy chọn những thẻ phù hợp nhất từ danh sách trên.";
            int x = 1;
            int a = 2;
            var schema = new ResponseSchema
            {

                Type = "OBJECT",
                Properties = new Dictionary<string, SchemaProperty>
                {
                    { "tags", new SchemaProperty { Type = "ARRAY", Items = new SchemaProperty { Type = "STRING" } } }
                }
            };

            var payload = new
            {
                Contents = new[] { new { Parts = new[] { new { Text = userPrompt } } } },
                SystemInstruction = new { Parts = new[] { new { Text = systemPrompt } } },
                GenerationConfig = new { ResponseMimeType = "application/json", ResponseSchema = schema }
            };

            var httpClient = _httpClientFactory.CreateClient("GeminiClient");
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            string? generatedText = null;

            try
            {
                var httpResponse = await httpClient.PostAsJsonAsync(apiUrl, payload, jsonSerializerOptions);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    // Ghi log lỗi nghiêm trọng hơn nếu cần
                    throw new Exception($"Gemini API returned status code {httpResponse.StatusCode}");
                }

                var geminiResponse = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(jsonSerializerOptions);
                generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nghiêm trọng hơn nếu cần
                return new List<string>();
            }
            // ----------------------------------------------------

            SuggestedTagsResponse? tagsResponse = null;
            List<string> selectedTagsFromAi = new List<string>();
            try
            {
                tagsResponse = JsonSerializer.Deserialize<SuggestedTagsResponse>(generatedText, jsonSerializerOptions);
                selectedTagsFromAi = tagsResponse?.Tags ?? new List<string>();
            }
            catch (JsonException jsonEx)
            {
                return new List<string>();
            }

            var finalSuggestions = selectedTagsFromAi
                                    .Select(tag => tag?.Trim())
                                    .Where(tag => !string.IsNullOrEmpty(tag) && existingTagNamesList.Contains(tag, StringComparer.OrdinalIgnoreCase))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();


            return finalSuggestions;
        }

        private async Task<List<TagDto>> GetExistingTagsAsync()
        {
            var coreApiUrl = _configuration["CoreApiBaseUrl"];
            if (string.IsNullOrEmpty(coreApiUrl))
            {
                throw new InvalidOperationException("CoreApiBaseUrl is not configured.");
            }

            var client = _httpClientFactory.CreateClient("CoreApiClient");
            var apiUrl = $"{coreApiUrl.TrimEnd('/')}/api/Tag";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>(options);
                    return tags ?? new List<TagDto>();
                }
                else
                {
                    return new List<TagDto>();
                }
            }
            catch (Exception ex)
            {
                return new List<TagDto>();
            }
        }
    }
}

