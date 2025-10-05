using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViaSignal.Dtos;
using ViaSignal.ViaServices.ElevenLabs;
using ViaSignal.ViaServices.MetisAi;
using ViaSignal.ViaServices.SpeechToText;

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

            var elevenClient = new ElevenLabsClient();


            string text = await elevenClient.SpeechToTextAsync(audioBytes);

            var ai = new MetisAiClient();
            var result = await ai.SendMessageAsync(
                text: text
            );

            result.Machine ??= "";
            result.Problem ??= "";
            result.Brand ??= "";
            result.City ??= "";
            result.Province ??= "";
            result.Address ??= "";
            result.PhoneNumber ??= "";
            result.Target ??= "";
            result.ToneOfVoice ??= "";
            result.Text ??= "";
            result.Response ??= "";

            voiceData.PhoneNumber = string.IsNullOrWhiteSpace(voiceData.PhoneNumber)
                ? result.PhoneNumber
                : voiceData.PhoneNumber;
            voiceData.Target = string.IsNullOrWhiteSpace(voiceData.Target) ? result.Target : voiceData.Target;
            voiceData.ToneOfVoice = string.IsNullOrWhiteSpace(voiceData.ToneOfVoice)
                ? result.ToneOfVoice
                : voiceData.ToneOfVoice;
            voiceData.Text = string.IsNullOrWhiteSpace(voiceData.Text) ? result.Text : voiceData.Text;
            voiceData.Response = string.IsNullOrWhiteSpace(voiceData.Response) ? result.Response : voiceData.Response;
            voiceData.Machine = string.IsNullOrWhiteSpace(voiceData.Machine) ? result.Machine : voiceData.Machine;
            voiceData.Problem = string.IsNullOrWhiteSpace(voiceData.Problem) ? result.Problem : voiceData.Problem;
            voiceData.Brand = string.IsNullOrWhiteSpace(voiceData.Brand) ? result.Brand : voiceData.Brand;
            voiceData.City = string.IsNullOrWhiteSpace(voiceData.City) ? result.City : voiceData.City;
            voiceData.Province = string.IsNullOrWhiteSpace(voiceData.Province)
                ? result.Province
                : voiceData.Province;
            voiceData.Address = string.IsNullOrWhiteSpace(voiceData.Address) ? result.Address : voiceData.Address;

            Console.WriteLine($"📋 Updated VoiceData: {JsonSerializer.Serialize(voiceData)}");


            await elevenClient.GenerateAsync(
                voiceId: "cgSgspJ2msm6clMCkdW9",
                text: voiceData.Response,
                outputFile: "output_http.mp3"
            );
            byte[] audioBytes1 =
                await System.IO.File.ReadAllBytesAsync(
                    @"C:\Users\Tanin\RiderProjects\ViaSignal\ViaSignal\output_http.mp3");

            if (!string.IsNullOrWhiteSpace(voiceData.Machine) &&
                !string.IsNullOrWhiteSpace(voiceData.Problem) &&
                !string.IsNullOrWhiteSpace(voiceData.Brand) &&
                !string.IsNullOrWhiteSpace(voiceData.City) &&
                !string.IsNullOrWhiteSpace(voiceData.Province) &&
                !string.IsNullOrWhiteSpace(voiceData.Address))
            {
                var client1 = new HttpClient();
                var response = await client1.PostAsJsonAsync(submitUrl, voiceData);
            }

            await webSocket.SendAsync(new ArraySegment<byte>(audioBytes1), WebSocketMessageType.Binary, true,
                CancellationToken.None);

            Console.WriteLine($"✅ Session updated & sent {audioBytes1.Length} bytes to client");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in ProcessAudioMessage: {ex.Message}");
        }
    }
}