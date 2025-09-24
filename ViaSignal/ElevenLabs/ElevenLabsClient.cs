using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ViaSignal.ElevenLabs;

public class ElevenLabsClient
{
    private readonly string _apiKey;
    private readonly HttpClient _http;

    public ElevenLabsClient(string apiKey = "7b53751763e9f67fc643eb39b105254e8da91a00cd7a09d1534be624eff6e462")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _http = new HttpClient { BaseAddress = new Uri("https://api.elevenlabs.io") };
        _http.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
    }


    public async Task GenerateAsync(string voiceId, string text, string outputFile, string modelId = "eleven_v3",
        string format = "mp3_44100_128")
    {
        var body = new
        {
            text = text,
            model_id = modelId
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"/v1/text-to-speech/{voiceId}?output_format={format}";

        using var resp = await _http.PostAsync(url, content);
        resp.EnsureSuccessStatusCode();

        await using var fs = File.Create(outputFile);
        await using var rs = await resp.Content.ReadAsStreamAsync();
        await rs.CopyToAsync(fs);

        Console.WriteLine($"[OK] Audio saved to {outputFile}");
    }

    /// <summary>
    /// استریم متن به گفتار (WebSocket)
    /// </summary>
    public async Task StreamAsync(string voiceId, string text, string outputFile, string modelId = "eleven_v3",
        string format = "mp3_44100_128", CancellationToken ct = default)
    {
        var uri = new Uri(
            $"wss://api.elevenlabs.io/v1/text-to-speech/{voiceId}/stream-input?model_id={modelId}&output_format={format}");
        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("xi-api-key", _apiKey);

        await ws.ConnectAsync(uri, ct);

        // init message
        var initMsg = new { text = "" };
        await SendJson(ws, initMsg, ct);

        // publish message
        var publishMsg = new { text = text, try_trigger_generation = true };
        await SendJson(ws, publishMsg, ct);

        await using var fs = File.Create(outputFile);

        var buffer = new byte[8192];
        while (ws.State == WebSocketState.Open)
        {
            var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var msg = Encoding.UTF8.GetString(ms.ToArray());
            try
            {
                using var doc = JsonDocument.Parse(msg);
                if (doc.RootElement.TryGetProperty("audio", out var audio))
                {
                    var bytes = Convert.FromBase64String(audio.GetString());
                    await fs.WriteAsync(bytes, 0, bytes.Length, ct);

                    if (doc.RootElement.TryGetProperty("isFinal", out var isFinal) && isFinal.GetBoolean())
                    {
                        Console.WriteLine("[DONE] Stream finished.");
                        break;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Non-JSON message received.");
            }
        }
    }

    private static async Task SendJson(ClientWebSocket ws, object obj, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }
}