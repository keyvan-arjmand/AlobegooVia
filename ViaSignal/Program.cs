using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViaSignal;
using ViaSignal.Dtos;
using ViaSignal.ViaServices.ElevenLabs;
using ViaSignal.ViaServices.MetisAi;
using ViaSignal.ViaServices.SpeechToText;
using ViaSignal.WebSockets;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

#region tests

var elevenClient = new ElevenLabsClient();

//
// await elevenClient.GenerateAsync(
//     voiceId: "JBFqnCBsd6RMkjVDRZzb",
//     text:
//     "سلام چطوری",
//     outputFile: "output_http.mp3"
// );
// byte[] audioBytes = System.IO.File.ReadAllBytes(@"C:\Voip\09116583118.wav");
//
// var a = await elevenClient.SpeechToTextAsync(audioBytes);
// Console.WriteLine(a);
// var ai = new MetisAiClient();
// var result = await ai.SendMessageAsync(
//     text: "سلام، چطوری چخبر چیکارا میکنی! "
// );


// var client = new PersianSpeechToText();
//
// // خواندن فایل صوتی
//
// // ارسال صدا و گرفتن متن
// string text = await client.SendVoiceAsync(audioBytes);
//
// Console.WriteLine("متن تبدیل شده: " + text);

#endregion


app.UseWebSockets();


var _sessions = new ConcurrentDictionary<string, VoiceData>();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    var sessionId = Guid.NewGuid().ToString();
    var voiceData = new VoiceData { SessionId = sessionId };
    _sessions.TryAdd(sessionId, voiceData);

    Console.WriteLine($"🆔 Created new session: {sessionId}");

    var sessionBytes = Encoding.UTF8.GetBytes(sessionId);

    await webSocket.SendAsync(new ArraySegment<byte>(sessionBytes), WebSocketMessageType.Text, true,
        CancellationToken.None);

    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "via", "voices", "Via1.mp3");
    if (File.Exists(filePath))
    {
        var audioBytes = await File.ReadAllBytesAsync(filePath);

        await webSocket.SendAsync(new ArraySegment<byte>(audioBytes), WebSocketMessageType.Binary, true,
            CancellationToken.None);

        Console.WriteLine($"🔊 Sent welcome audio to client ({audioBytes.Length} bytes)");
    }

    await webSocket.HandleAudio(voiceData);

    _sessions.TryRemove(sessionId, out _);
});

app.Run();