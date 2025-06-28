using System;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public static class FlowPredictor
{
    private static readonly InferenceSession _session = new InferenceSession("flow_model.onnx");

    public static int GetDaysRemaining(float flowRate)
    {
        // Prepare input tensor (1x1)
        var inputTensor = new DenseTensor<float>(new[] { flowRate }, new[] { 1, 1 });
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);
        var output = results.First().AsEnumerable<float>().First();

        // Round to int, clamp to minimum 0
        return Math.Max(0, (int)Math.Round(output));
    }
}
