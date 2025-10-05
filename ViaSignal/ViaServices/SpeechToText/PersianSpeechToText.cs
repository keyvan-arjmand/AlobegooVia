using System.Net.Http.Headers;
using System.Speech.Recognition;
using System.Text.Json;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SpeechRecognizer = Microsoft.CognitiveServices.Speech.SpeechRecognizer;

namespace ViaSignal.ViaServices.SpeechToText;
public class PersianSpeechToText
{
    private readonly HttpClient _httpClient;

    public PersianSpeechToText()
    {
        // ✅ مطمئن شو FastAPI روی همین پورت در حال اجراست
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:8081")
        };
    }

    public async Task<string> SendVoiceAsync(byte[] audioData, string payload = "")
    {
        using var content = new MultipartFormDataContent();

        // ✅ ساخت محتوای فایل
        var audioContent = new ByteArrayContent(audioData);
        audioContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

        // ✅ افزودن فایل و سایر داده‌ها
        content.Add(audioContent, "file", "voice.wav");
        content.Add(new StringContent(payload ?? ""), "payload");

        // ✅ مسیر endpoint (نسبت به BaseAddress)
        var url = "/azinllm/voice/SpeechToText";

        // ✅ ارسال درخواست
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode(); // اگر خطا داشت Exception می‌اندازد

        // ✅ خواندن پاسخ
        var responseText = await response.Content.ReadAsStringAsync();

        return responseText;
    }
}
