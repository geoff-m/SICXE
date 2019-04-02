using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXEAssembler
{
    class ToolMain
    {
        public static void Main(string[] args)
        {
#if DEBUG
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
#endif
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Expected at least 1 argument: Path to assembly file");
                return;
            }

            var assembler = new Assembler();
            // Use the entry point of the first file on the command line.
            for (int i = 0; i < args.Length; ++i)
                assembler.AddInputFile(args[i], i == 0);
            var outputPath = args[0] + ".obj";
            try
            {
                var bin = assembler.AssembleBinary();
                // Write the binary to file.
                bin.WriteOBJ(outputPath);
            }
            catch (InvalidProgramException ex)
            {
                Console.Error.WriteLine(ex.Message);
#if DEBUG
                Console.WriteLine("DEBUG: Press enter to exit...");
                Console.ReadLine();
#endif
                return;
            }

            Console.WriteLine($"Assembly successful. Wrote \"{outputPath}\".");
#if DEBUG
            Console.WriteLine("DEBUG: Press enter to exit...");
            Console.ReadLine();
#endif
        }
    }
}
