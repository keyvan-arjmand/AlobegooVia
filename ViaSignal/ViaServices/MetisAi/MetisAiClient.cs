using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ViaSignal.Dtos;

namespace ViaSignal.ViaServices.MetisAi;

public class MetisAiClient
{
    private static readonly HttpClient client = new HttpClient();

    public async Task<VoiceData> SendMessageAsync(string text)
    {
        const string urlAi = "https://api.metisai.ir/api/v1/chat/session";
        try
        {
            // Step 1: Create session
            var payloadAi = new
            {
                botId = "c372b1cd-c849-4a41-91bc-25ab591879b6",
                user = (string)null,
                initialMessages = (string)null
            };

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "tpsg-fZjznCMESRkQnVIrylqg8zZA4QvUVWn");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(JsonSerializer.Serialize(payloadAi), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(urlAi, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var sessionObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var sessionId = sessionObj.GetProperty("id").GetString();

            // Step 2: Send message
            var messageUrl = $"https://api.metisai.ir/api/v1/chat/session/{sessionId}/message";

            var payloadMessage = new
            {
                message = new
                {
                    content = text,
                    type = "USER"
                }
            };

            var messageContent = new StringContent(JsonSerializer.Serialize(payloadMessage), Encoding.UTF8,
                "application/json");
            var responseAi = await client.PostAsync(messageUrl, messageContent);
            responseAi.EnsureSuccessStatusCode();

            var responseText = await responseAi.Content.ReadAsStringAsync();
            var contentObj = JsonSerializer.Deserialize<JsonElement>(responseText);

            string contentStr = contentObj.GetProperty("content").GetString() ?? "";
            contentStr = contentStr.Trim();

            // پاک‌سازی backtick و ```json
            if (contentStr.StartsWith("```"))
            {
                int start = contentStr.IndexOf('{');
                int end = contentStr.LastIndexOf('}');
                if (start >= 0 && end > start)
                    contentStr = contentStr.Substring(start, end - start + 1);
            }
            contentStr = contentStr.Replace("`", "");

            // Deserialize مستقیم به کلاس
            var voiceData = JsonSerializer.Deserialize<VoiceData>(contentStr);

            return voiceData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new VoiceData();
        }
    }
}