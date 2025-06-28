using System;
using System.Diagnostics;
using System.IO;
/*
public static class WeldPredictor
{
    public static int GetWeldsRemaining(float temperature)
    {
        // Relative path to Python script (should be in the same folder as the .exe or in a subfolder)
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ml_weld_predict.py");
        // Use "python" directly assuming Python is added to system PATH
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" {temperature}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                throw new Exception("Failed to start Python process.");

            string output = process.StandardOutput.ReadToEnd();
            Debug.WriteLine($"[Python Output]: {output}");  // <-- Add this

            process.WaitForExit();

            if (int.TryParse(output.Trim(), out int weldsRemaining))
                return weldsRemaining;

            return 0; // Default fallback if parsing fails
        }
        catch (Exception ex)
        {
            // Log or handle exceptions as needed
            Debug.WriteLine($"Error running Python script: {ex.Message}");
            return 0;
        }
    }
}
public static class WeldPredictor
{
    public static int GetWeldsRemaining(float temperature)
    {
        const int MaxTemperature = 40;
        int remaining = (int)(MaxTemperature - temperature);
        return Math.Max(0, remaining);
    }
}*/


using System;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public static class WeldPredictor
{
    private static readonly InferenceSession _session = new InferenceSession("weld_model.onnx");

    public static int GetWeldsRemaining(float temperature)
    {
        // Prepare input tensor (1x1)
        var inputTensor = new DenseTensor<float>(new[] { temperature }, new[] { 1, 1 });
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);
        var output = results.First().AsEnumerable<float>().First();

        // Round and clamp to 0
        return Math.Max(0, (int)Math.Round(output));
    }
}


