using System;
using System.Threading.Tasks;
using OBDReader.Core.Testing;

namespace OBDReader.TestRunner
{
    /// <summary>
    /// Test Runner - Executes comprehensive OBD-II application tests
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
   ╔═══════════════════════════════════════════════════════════════╗
   ║                                                               ║
   ║         OBD-II DIAGNOSTIC TOOL - TEST SUITE v1.0             ║
   ║         Comprehensive Testing with Mock Interface            ║
   ║                                                               ║
   ║         For: 2008 GMC Yukon Denali                           ║
   ║         Protocol: ISO 15765-4 (CAN bus)                      ║
   ║                                                               ║
   ╚═══════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("This test suite validates all OBD-II functionality using a");
            Console.WriteLine("simulated vehicle interface. No physical hardware required.");
            Console.WriteLine();
            Console.WriteLine("Press ENTER to start tests...");
            Console.ReadLine();
            Console.Clear();

            try
            {
                var testSuite = new OBDReaderTests();
                bool allPassed = await testSuite.RunAllTestsAsync();

                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();

                return allPassed ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                  FATAL ERROR IN TEST SUITE                    ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine($"\nException: {ex.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
                Console.WriteLine("\nPress ENTER to exit...");
                Console.ReadLine();
                return 1;
            }
        }
    }
}
