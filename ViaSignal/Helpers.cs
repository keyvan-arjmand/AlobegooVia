using ViaSignal.Dtos;

namespace ViaSignal;

public static class Helpers
{
    public static async Task<byte[]> ValidateVoiceData(this VoiceData voiceData)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "via", "voices");

        var voiceFiles = new Dictionary<string, string>
        {
            { nameof(voiceData.Machine), Path.Combine(basePath, "ViaMachin.mp3") },
            { nameof(voiceData.Problem), Path.Combine(basePath, "ViaProblem.mp3") },
            { nameof(voiceData.Brand), Path.Combine(basePath, "ViaBrand.mp3") },
            { nameof(voiceData.City), Path.Combine(basePath, "ViaCity.mp3") },
            { nameof(voiceData.Province), Path.Combine(basePath, "ViaPrv.mp3") },
            { nameof(voiceData.Address), Path.Combine(basePath, "ViaAddress.mp3") }
        };

        bool allNullOrEmpty = true;
        string firstEmptyFieldFile = null;

        foreach (var kvp in voiceFiles)
        {
            var prop = voiceData.GetType().GetProperty(kvp.Key);
            var value = prop?.GetValue(voiceData) as string;

            if (!string.IsNullOrEmpty(value))
            {
                allNullOrEmpty = false;
                continue;
            }

            // ذخیره اولین فایل مربوط به فیلد خالی
            if (firstEmptyFieldFile == null && File.Exists(kvp.Value))
                firstEmptyFieldFile = kvp.Value;
        }

        if (allNullOrEmpty)
        {
            var errorFile = Path.Combine(basePath, "ViaNotValid.mp3");
            if (File.Exists(errorFile))
                return await File.ReadAllBytesAsync(errorFile);
        }

        if (firstEmptyFieldFile != null)
            return await File.ReadAllBytesAsync(firstEmptyFieldFile);

        // همه پر بودن → فایل موفقیت
        var successFile = Path.Combine(basePath, "ViaFinal.mp3");
        if (File.Exists(successFile))
            return await File.ReadAllBytesAsync(successFile);

        // اگر هیچ فایلی پیدا نشد
        var fallbackFile = Path.Combine(basePath, "ViaNotValid.mp3");
        return await File.ReadAllBytesAsync(fallbackFile);
    }

}