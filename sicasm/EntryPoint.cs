using System;
using System.IO;

namespace SICXE
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Expected 1 argument: Path to assembly file");
                return;
            }
            var path = args[0];
            try
            {
                if (Program.TryParse(path, out Program myProgram))
                {
                    var assembler = new Assembler(myProgram);
                    var outpath = path + ".lst";
                    if (assembler.PassOne(outpath))
                    {
                        Console.WriteLine($"\nAssembly pass one succeeded. Listing file written to \"{outpath}\"");
                    }
                    else
                    {
                        Console.WriteLine("\nAssembly pass one failed.");
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void WriteRandomProgram(string path, int lineCount)
        {
            Console.WriteLine($"Generating random {lineCount} line program...");
            StreamWriter write = null;
            Program rp = null;
            try
            {
                write = new StreamWriter(path, false);
                var pgen = new RandomProgramGenerator();
                rp = pgen.MakeRandomProgram(lineCount);
                foreach (var line in rp)
                {
                    //Console.WriteLine(line.ToString(10));
                    write.WriteLine(line.ToString(10));
                }
            }
            finally
            {
                if (write != null)
                    write.Dispose();
            }
            Console.WriteLine($"Done. Program has {rp.Count} lines.");
        }

        static void TestProgramParser()
        {
            const string TEST_PROGRAM_PATH = "test-prog.txt";
            const int TEST_PROGRAM_SIZE = 500;

            WriteRandomProgram(TEST_PROGRAM_PATH, TEST_PROGRAM_SIZE);

            Console.WriteLine($"\nParsing...");
            if (Program.TryParse(TEST_PROGRAM_PATH, out Program parsed))
            {
                Console.WriteLine($"Parsing {TEST_PROGRAM_PATH} succeeded.");

                var asm = new Assembler(parsed);
                if (asm.PassOne(TEST_PROGRAM_PATH + ".lst"))
                {
                    Console.WriteLine("Assembly pass one succeeded.");
                    if (asm.PassTwo())
                    {
                        Console.WriteLine("Assembly pass two succeeded.");
                    }
                    else
                    {
                        Console.WriteLine("Assembly pass two failed.");
                    }
                }
                else
                {
                    Console.WriteLine("Assembly pass one failed.");
                }
            }
            else
            {
                Console.WriteLine($"Parsing {TEST_PROGRAM_PATH} failed.");
            }


        }
    }
}
