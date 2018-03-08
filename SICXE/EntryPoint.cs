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
    }
}
