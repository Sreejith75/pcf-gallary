using AppWeaver.AIBrain.IntegrationTest;

namespace AppWeaver.AIBrain.IntegrationTest;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Execute the End-to-End Pipeline Integration Test
        return await EndToEndPipelineTest.RunAsync();
    }
}
