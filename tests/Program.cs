namespace Alissa.Tests
{
    /// <summary>
    /// Test runner for Alissa refactoring validation.
    /// Runs tests for memory pipeline, indexing, prompt building, configuration, and Phase 1 improvements.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║  ALISSA ARCHITECTURE REFACTOR TESTS    ║");
            Console.WriteLine("╚════════════════════════════════════════╝");

            try
            {
                await MemoryPipelineTests.RunAllTests();
                IndexingTests.RunAllTests();
                PromptBuilderTests.RunAllTests();
                ConfigurationTests.RunAllTests();
                Phase1ImprovementTests.RunAllTests();

                Console.WriteLine("\n╔════════════════════════════════════════╗");
                Console.WriteLine("║  ✓ ALL TESTS COMPLETED SUCCESSFULLY    ║");
                Console.WriteLine("╚════════════════════════════════════════╝\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n╔════════════════════════════════════════╗");
                Console.WriteLine($"║  ✗ TEST FAILURE: {ex.Message.Substring(0, Math.Min(25, ex.Message.Length)).PadRight(25)}  ║");
                Console.WriteLine($"╚════════════════════════════════════════╝\n");
            }
        }
    }
}

