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
            TestProgramParser();

            //const string GOOGLE_DRIVE_PATH = @"C:\Users\geoff\Google Drive\";
            const string GOOGLE_DRIVE_PATH = @"E:\Google Drive\";

            const string PROGRAM_PATH = @"Intro to System Software\asms\copy-tix-add.asm";
            const string LIST_DIRECTORY = @"Intro to System Software\lsts\";
            Directory.CreateDirectory(GOOGLE_DRIVE_PATH + LIST_DIRECTORY);
            string LIST_PATH = $"{LIST_DIRECTORY}{Path.GetFileNameWithoutExtension(PROGRAM_PATH)}.lst.txt";
            //const string PROGRAM_PATH = @"Intro to System Software\asms\small.txt";

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

                //var myMachine = new vsic.Machine();
            }

        }

        static void TestProgramParser()
        {
            const string TEST_PROGRAM_PATH = "test-prog.txt";
            const int TEST_PROGRAM_SIZE = 100000;
            StreamWriter write = null;
            try
            {
                write = new StreamWriter(TEST_PROGRAM_PATH, false);
                var pgen = new RandomProgramGenerator();
                Console.WriteLine($"Generating random {TEST_PROGRAM_SIZE} line program...");
                var rp = pgen.MakeRandomProgram(TEST_PROGRAM_SIZE);
                foreach (var line in rp)
                {
                    //Console.WriteLine(line.ToString(10));
                    write.WriteLine(line.ToString(10));
                }
                write.Dispose();

                Console.WriteLine($"Done.\nParsing...");
                if (Program.TryParse(TEST_PROGRAM_PATH, out Program parsed))
                {
                    Console.WriteLine($"Parsing {TEST_PROGRAM_PATH} succeeded.");
                }
                 else
                {
                    Console.WriteLine($"Parsing {TEST_PROGRAM_PATH} failed.");
                }
            }
            finally
            {
                if (write != null)
                    write.Dispose();
            }
        }
    }
}
