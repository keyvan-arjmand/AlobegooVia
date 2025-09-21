using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViaSignal.Dtos;

namespace ViaSignal.WebSockets;

public static class SocketService
{
    private const string submitUrl = "https://api.alobegoo.com/ai-noauth/azinllm/voice/submit/175909";

    private const string apiUrlFill = "https://api.alobegoo.com/ai-noauth/azinllm/voice/fill";

    public static async Task HandleAudio(this WebSocket webSocket, VoiceData voiceData)
    {
        var buffer = new byte[8 * 1024];
        using var ms = new MemoryStream();

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.CloseStatus.HasValue)
            {
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription,
                    CancellationToken.None);
                break;
            }
            
            ms.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage) continue;
            var audioBytes = ms.ToArray();
            ms.SetLength(0);
            if (audioBytes.Length > 0)
            {
                await ProcessAudioMessage(webSocket, voiceData, audioBytes);
            }
        }
    }

    private static async Task ProcessAudioMessage(WebSocket webSocket, VoiceData voiceData,
        byte[] audioBytes)
    {
        try
        {
            Console.WriteLine($"🎧 Processing audio for session: {voiceData.SessionId} ({audioBytes.Length} bytes)");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(fileContent, "file", "audio.wav");

            var responseFill = await client.PostAsync(apiUrlFill, content);
            responseFill.EnsureSuccessStatusCode();

            var responseText = await responseFill.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                Console.WriteLine("❌ Empty response from API");
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // نادیده گرفتن تفاوت حروف بزرگ و کوچک
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // نادیده گرفتن پراپرتی‌های null
            };

            var newVoiceData = JsonSerializer.Deserialize<VoiceData>(responseText, options);
            if (newVoiceData == null)
            {
                Console.WriteLine("❌ Failed to deserialize API response");
                return;
            }

            newVoiceData.Machine ??= "";
            newVoiceData.Problem ??= "";
            newVoiceData.Brand ??= "";
            newVoiceData.City ??= "";
            newVoiceData.Province ??= "";
            newVoiceData.Address ??= "";

            voiceData.Machine = string.IsNullOrWhiteSpace(voiceData.Machine) ? newVoiceData.Machine : voiceData.Machine;
            voiceData.Problem = string.IsNullOrWhiteSpace(voiceData.Problem) ? newVoiceData.Problem : voiceData.Problem;
            voiceData.Brand = string.IsNullOrWhiteSpace(voiceData.Brand) ? newVoiceData.Brand : voiceData.Brand;
            voiceData.City = string.IsNullOrWhiteSpace(voiceData.City) ? newVoiceData.City : voiceData.City;
            voiceData.Province = string.IsNullOrWhiteSpace(voiceData.Province)
                ? newVoiceData.Province
                : voiceData.Province;
            voiceData.Address = string.IsNullOrWhiteSpace(voiceData.Address) ? newVoiceData.Address : voiceData.Address;

            Console.WriteLine($"📋 Updated VoiceData: {JsonSerializer.Serialize(voiceData)}");

            var validVoice = await voiceData.ValidateVoiceData();
            if (validVoice == null || validVoice.Length == 0)
            {
                Console.WriteLine("❌ Validation failed or empty validVoice");
                return;
            }

            var response = await client.PostAsJsonAsync(submitUrl, voiceData);

            await webSocket.SendAsync(new ArraySegment<byte>(validVoice), WebSocketMessageType.Binary, true,
                CancellationToken.None);
            Console.WriteLine($"✅ Session updated & sent {validVoice.Length} bytes to client");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in ProcessAudioMessage: {ex.Message}");
        }
    }
}