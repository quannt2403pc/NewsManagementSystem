using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class LoginResponseViewModel
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
