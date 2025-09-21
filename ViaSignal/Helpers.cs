using ViaSignal.Dtos;

namespace ViaSignal;

public static class Helpers
{
    public static async Task<byte[]> ValidateVoiceData(this VoiceData voiceData)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "via", "voices");

        VoiceState state;

        // بررسی همه خالی بودن
        if (string.IsNullOrWhiteSpace(voiceData.Machine) &&
            string.IsNullOrWhiteSpace(voiceData.Problem) &&
            string.IsNullOrWhiteSpace(voiceData.Brand) &&
            string.IsNullOrWhiteSpace(voiceData.City) &&
            string.IsNullOrWhiteSpace(voiceData.Province) &&
            string.IsNullOrWhiteSpace(voiceData.Address))
        {
            state = VoiceState.Invalid; // همه خالی
        }
        else
        {
            state = voiceData switch
            {
                _ when string.IsNullOrWhiteSpace(voiceData.Machine) => VoiceState.WaitingForMachine,
                _ when string.IsNullOrWhiteSpace(voiceData.Problem) => VoiceState.WaitingForProblem,
                _ when string.IsNullOrWhiteSpace(voiceData.Brand) => VoiceState.WaitingForBrand,
                _ when string.IsNullOrWhiteSpace(voiceData.City) => VoiceState.WaitingForCity,
                _ when string.IsNullOrWhiteSpace(voiceData.Province) => VoiceState.WaitingForProvince,
                _ when string.IsNullOrWhiteSpace(voiceData.Address) => VoiceState.WaitingForAddress,
                _ => VoiceState.Completed
            };
        }

        // فایل صوتی متناظر با State
        var fileName = state switch
        {
            VoiceState.WaitingForMachine => "ViaMachin.mp3",
            VoiceState.WaitingForProblem => "ViaProblem.mp3",
            VoiceState.WaitingForBrand => "ViaBrand.mp3",
            VoiceState.WaitingForCity => "ViaCity.mp3",
            VoiceState.WaitingForProvince => "ViaPrv.mp3",
            VoiceState.WaitingForAddress => "ViaAddress.mp3",
            VoiceState.Completed => "ViaFinal.mp3",
            VoiceState.Invalid => "ViaNotValid.mp3",
            _ => "ViaNotValid.mp3"
        };

        var filePath = Path.Combine(basePath, fileName);

        // اگر فایل موجود نبود → fallback
        if (!File.Exists(filePath))
            filePath = Path.Combine(basePath, "ViaNotValid.mp3");

        return await File.ReadAllBytesAsync(filePath);
    }

    private enum VoiceState
    {
        WaitingForMachine,
        WaitingForProblem,
        WaitingForBrand,
        WaitingForCity,
        WaitingForProvince,
        WaitingForAddress,
        Completed,
        Invalid
    }
}