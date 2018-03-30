using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SICXE
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            //TestProgramParser();

            const string GOOGLE_DRIVE_PATH = @"C:\Users\geoff\Google Drive\";
            //const string GOOGLE_DRIVE_PATH = @"E:\Google Drive\";

            const string PROGRAM_PATH = @"Intro to System Software\asms\small.txt";
            const string LIST_DIRECTORY = @"Intro to System Software\lsts\";
            Directory.CreateDirectory(GOOGLE_DRIVE_PATH + LIST_DIRECTORY);
            string LIST_PATH = $"{LIST_DIRECTORY}{Path.GetFileNameWithoutExtension(PROGRAM_PATH)}.lst.txt";

            if (Program.TryParse(GOOGLE_DRIVE_PATH + PROGRAM_PATH, out Program myProgram))
            {
                for (int i = 0; i < myProgram.Count; ++i)
                {
                    Console.WriteLine($"{myProgram[i].ToString()}");
                }
                var assembler = new Assembler(myProgram);
                if (assembler.PassOne(GOOGLE_DRIVE_PATH + LIST_PATH))
                {
                    Console.WriteLine("\nAssembly pass one succeeded.");
                }
                else
                {
                    Console.WriteLine("\nAssembly pass one failed.");
                }
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
