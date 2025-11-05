using System.Text.Json.Serialization;

namespace AiApi.Services.Gemini
{
    // Các lớp này dùng để map (ánh xạ) dữ liệu JSON
    // gửi đi và nhận về từ Gemini API.

    // --- Cấu trúc Schema (để yêu cầu AI trả về JSON) ---
    public class GenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = "application/json";

        [JsonPropertyName("responseSchema")]
        public ResponseSchema ResponseSchema { get; set; }
    }

    public class ResponseSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "OBJECT";

        [JsonPropertyName("properties")]
        public Dictionary<string, SchemaProperty> Properties { get; set; }
    }

    public class SchemaProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SchemaProperty? Items { get; set; }
    }

    // --- Cấu trúc Response (kết quả trả về từ Gemini) ---
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    // --- Cấu trúc JSON chúng ta muốn AI trả về ---
    // (Khớp với { "tags": ["tag1", "tag2"] } )
    public class SuggestedTagsResponse
    {
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }
}
