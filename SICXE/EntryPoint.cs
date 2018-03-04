using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXE
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            //const string GOOGLE_DRIVE_PATH = @"C:\Users\geoff\Google Drive\";
            const string GOOGLE_DRIVE_PATH = @"E:\Google Drive\";
            //const string PROGRAM_PATH = @"Intro to System Software\asms\copy-tix-add.asm";
            const string PROGRAM_PATH = @"Intro to System Software\asms\small.txt";
            //var myProgram = Program.Parse(GOOGLE_DRIVE_PATH + @"Intro to System Software\asms\small.txt");

            if (Program.TryParse(GOOGLE_DRIVE_PATH + PROGRAM_PATH, out Program myProgram))
            {

                for (int i = 0; i < myProgram.Count; ++i)
                {
                    Console.WriteLine($"{myProgram[i].ToString()}");
                }

                var assembler = new Assembler(myProgram);
                if (assembler.TryAssemble(out Word[] myBinary))
                {
                    Console.WriteLine("\nAssembly succeeded.");
                }
                else
                {
                    Console.WriteLine("\nAssembly failed.");
                }

                //var myMachine = new vsic.Machine();
            }

        }
    }
}
