using System.Text;
using Microsoft.ML.Data;
using Microsoft.ML;
using MLModel_ConsoleApp1;

Console.OutputEncoding = Encoding.UTF8;

// کمکی برای نمایش راست به چپ (با معکوس کردن متن)
string ToRtl(string text)
{
    char[] arr = text.ToCharArray();
    Array.Reverse(arr);
    return new string(arr);
}

// نمونه دیتا
MLModel.ModelInput sampleData = new MLModel.ModelInput()
{
    Problem = "پمپ تخلیه کار نمیکنه",
    RowNumber= "پمپ تخلیه کار نمیکنه"

};

Console.WriteLine("=============== ML.NET Prediction ===============\n");

Console.WriteLine("📌 Input Data");
Console.WriteLine("----------------------------------------");
Console.WriteLine($"RowNumber : {ToRtl("2")}");
Console.WriteLine($"Problem   : {ToRtl(sampleData.Problem)}");
Console.WriteLine();

// پیش‌بینی با مدل
var sortedScoresWithLabel = MLModel.PredictAllLabels(sampleData);
// پیدا کردن بیشترین امتیاز
var bestPrediction = sortedScoresWithLabel
    .OrderByDescending(x => x.Value)
    .Take(3)
    .ToList();

Console.WriteLine("🎯 Best Prediction");
Console.WriteLine("----------------------------------------");
foreach (var item in bestPrediction)
{
    Console.WriteLine($"Class : {ToRtl(item.Key)}");
    Console.WriteLine($"Score : {item.Value}");
}

Console.WriteLine("\n================================================");
Console.WriteLine("✅ Process finished. Press any key to exit...");
Console.ReadKey();

