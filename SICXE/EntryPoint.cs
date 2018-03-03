using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXE
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            const string GOOGLE_DRIVE_PATH = @"C:\Users\geoff\Google Drive\";

            //var myProgram = Program.Parse(GOOGLE_DRIVE_PATH + @"Intro to System Software\asms\small.txt");

            if (Program.TryParse(GOOGLE_DRIVE_PATH + @"Intro to System Software\asms\copy-add.asm", out Program myProgram))
            {

                for (int i = 0; i < myProgram.Count; ++i)
                {
                    Console.WriteLine($"{myProgram[i].ToString()}");
                }


                var assembler = new Assembler(myProgram);
                if (assembler.TryAssemble(out Word[] myBinary))
                {
                    Console.WriteLine("Assembly succeeded.");
                }
                else
                {
                    Console.WriteLine("Assembly failed.");
                }

                //var myMachine = new vsic.Machine();
            }



        }
    }
}
