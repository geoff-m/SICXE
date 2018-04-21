using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SICXE
{
    class AssemblerToolMain
    {
        static void Main(string[] args)
        {
#if DEBUG
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
            Console.Error.WriteLine("####################  DEBUG BUILD  ####################");
#endif
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected 1 argument: Path to assembly file");
                return;
            }
            var path = args[0];
            try
            {
                Program myProgram = null;
                if (Program.TryParse(path, out myProgram))
                {
                    var assembler = new Assembler(myProgram);
                    var lstPath = path + ".lst";
                    var objPath = path + ".obj";
                    if (assembler.PassOne())
                    {
                        Console.Error.WriteLine($"\nAssembly pass one succeeded.");
                        //assembler.PrintSymbolTable();

                        if (assembler.PassTwo(lstPath))
                        {
                            Console.Error.WriteLine($"\nAssembly pass two succeeded.\nListing file written to \"{lstPath}\".");
                            assembler.Output.WriteOBJ(objPath);
                            Console.Error.WriteLine($"OBJ file written to \"{objPath}\".");
                        }
                        else
                        {
                            Console.Error.WriteLine("\nAssembly pass two failed.");
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("\nAssembly pass one failed.");
                    }
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }

        static void WriteRandomProgram(string path, int lineCount)
        {
            Console.Error.WriteLine($"Generating random {lineCount} line program...");
            StreamWriter write = null;
            Program rp = null;
            try
            {
                write = new StreamWriter(path, false);
                var pgen = new RandomProgramGenerator();
                rp = pgen.MakeRandomProgram(lineCount);
                foreach (var line in rp)
                {
                    //Console.Error.WriteLine(line.ToString(10));
                    write.WriteLine(line.ToString(10));
                }
            }
            finally
            {
                if (write != null)
                    write.Dispose();
            }
            Console.Error.WriteLine($"Done. Program has {rp.Count} lines.");
        }

        static void TestProgramParser()
        {
            const string TEST_PROGRAM_PATH = "test-prog.txt";
            const int TEST_PROGRAM_SIZE = 500;

            WriteRandomProgram(TEST_PROGRAM_PATH, TEST_PROGRAM_SIZE);

            Console.Error.WriteLine($"\nParsing...");
            Program parsed;
            if (Program.TryParse(TEST_PROGRAM_PATH, out parsed))
            {
                Console.Error.WriteLine($"Parsing {TEST_PROGRAM_PATH} succeeded.");

                var asm = new Assembler(parsed);
                if (asm.PassOne(TEST_PROGRAM_PATH + ".lst"))
                {
                    Console.Error.WriteLine("Assembly pass one succeeded.");
                    if (asm.PassTwo())
                    {
                        Console.Error.WriteLine("Assembly pass two succeeded.");
                    }
                    else
                    {
                        Console.Error.WriteLine("Assembly pass two failed.");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Assembly pass one failed.");
                }
            }
            else
            {
                Console.Error.WriteLine($"Parsing {TEST_PROGRAM_PATH} failed.");
            }


        }
    }
}
