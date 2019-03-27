using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace SICXEAssembler
{
    public class AssemblerToolMain
    {
        class AssemblerJobPart
        {
            public readonly string FilePath;
            public Program Program;
            public Assembler Assembler;
            public Binary Binary;
            public AssemblerJobPart(string filePath)
            {
                FilePath = filePath;
            }
        }

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

            var inputFilePaths = args;
            if (!CheckPathsOk(inputFilePaths))
                return;

            // Parse all source files as Programs.
            var files = new Dictionary<string, AssemblerJobPart>();
            try
            {
                foreach (var f in inputFilePaths)
                {
                    if (Program.TryParse(f, out Program prog))
                    {
                        files[f] = new AssemblerJobPart(f) { Program = prog };
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error parsing \"{f}\".");
                        return;
                    }
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return;
            }

            Console.Error.WriteLine();
            // Assemble each program.
            // We have to do this individually so that symbol names don't collide.

            // First do all pass ones. This discovers all symbol defs and refs.
            // Then we can satisfy imports before doing pass two.
            string currentFile = null;
            try
            {
                foreach (var f in files.Keys)
                {
                    currentFile = f; // So that it's accessible in the catch block.
                    var assembler = new Assembler(files[f].Program);
                    files[f].Assembler = assembler;
                    //var lstPath = f + ".lst";
                    //var objPath = f + ".obj";
                    Console.Error.WriteLine($"Assembling \"{f}\"...");
                    if (assembler.PassOne())
                    {
                        Console.Error.WriteLine($"Assembly pass one succeeded for {f}.");
                        assembler.PrintSymbolTable();
                    }
                    else
                    {
                        Console.Error.WriteLine($"\nAssembly pass one failed for {f}.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (currentFile != null)
                {
                    if (ex is System.Threading.ThreadAbortException)
                    {
                        Console.Error.WriteLine($"Aborted during assembling \"{currentFile}\".");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error assembling \"{currentFile}\": {ex.Message}");
                    }
                }
                else
                    Console.Error.WriteLine($"Error: {ex.Message}");
                return;
            }

            // Build table of all exported symbols.
            var exports = new Dictionary<string, ExportedSymbol>();
            foreach (var f in files.Keys)
            {
                var assemblyExportsTable = files[f].Assembler.Exports;
                foreach (var exportName in assemblyExportsTable.Keys)
                {
                    if (assemblyExportsTable[exportName] == null)
                    {
                        Console.Error.WriteLine($"Error: Symbol \"{exportName}\" is marked as exported by \"{files[f].FilePath}\" but it is not defined!");
                        return;
                    }
                    var expName = assemblyExportsTable[exportName].Name;
                    Debug.Assert(expName == exportName);
                    if (exports.TryGetValue(expName, out ExportedSymbol existing))
                    {
                        Console.Error.WriteLine($"Error: Symbol \"{expName}\" is exported both by \"{files[f].FilePath}\" and by \"{existing.OriginProgram.OriginFile}\"!");
                        return;
                    }
                    else
                    {
                        var baseAddr = files[f].Assembler.BaseAddress.Value;
                        exports[expName] = new ExportedSymbol(files[f].Program, baseAddr, assemblyExportsTable[exportName]);
                    }
                }
            }

            Console.WriteLine("Public symbols\n-----------------------------------");
            ExportedSymbol.PrintTable(exports.Values.ToList());

            // Do assembly pass two.
            foreach (var f in files.Keys)
            {
                var asm = files[f].Assembler;

                asm.GiveImports(exports); // Inform assembler of symbols exported by others.

                if (asm.PassTwo(null))
                {
                    Console.Error.WriteLine($"Assembly pass two succeeded for {f}.");
                    //Console.Error.WriteLine($"Listing file written to \"{lstPath}\".");
                    //assembler.Output.WriteOBJ(objPath);
                    //Console.Error.WriteLine($"OBJ file written to \"{objPath}\".");
                    files[f].Binary = asm.Output;
                }
                else
                {
                    Console.Error.WriteLine($"\nAssembly pass two failed for {f}.");
                    return;
                }
            }

            // Attempt to combine the binaries.

            // First check for overlap.
            foreach (var f in files.Keys)
            {
                var fbin = files[f].Binary;
                foreach (var g in files.Keys)
                {
                    if (f == g)
                        continue;
                    var gbin = files[g].Binary;
                    if (fbin.CanCombineWithoutRelocating(gbin))
                        continue;
                    var overlap = fbin.FindCollidingSegments(gbin);
                    Console.Error.WriteLine($"Error: Code or data in {f} overlaps code or data in {g}.");
                    if (overlap != null)
                    {

                        var frange = fbin.GetMaximumRange();
                        var grange = gbin.GetMaximumRange();
                        Console.Error.WriteLine($"{Path.GetFileName(f)}:\n\tStart: 0x{frange.Start:X6}\n\tEnd: 0x{frange.Stop:X6}");
                        Console.Error.WriteLine($"\n{Path.GetFileName(g)}:\n\tStart: 0x{grange.Start:X6}\n\tEnd: 0x{grange.Stop:X6}");
                        return;
                    }
                    else
                    {
                        //Debug.Fail("CanCombineWithoutRelocating returned true but FindCollidingSegments returned null!");
                        // This can be caused by the collision happening where there's a RESW.
                        // CCWR will detect it but FCS will not because RESW does not actually create a segment.
                    }
                    return;
                }
            }

            // We got this far because they didn't overlap.
            // Combine the binaries.
            var bigBin = new Binary();
            foreach (var f in files.Keys)
            {
                var bin = files[f].Binary;

                foreach (var seg in bin.Segments)
                {
                    bool addresult = bigBin.AddSegment(seg);
                    Debug.Assert(addresult);
                }
            }

            bigBin.EntryPoint = files[inputFilePaths[0]].Binary.EntryPoint; // Use the entry point of the first file on the command line.

            // Write the binary to file.
            //bigBin.WriteLST(files[0] + ".lst");
            bigBin.WriteOBJ(inputFilePaths[0] + ".obj");


        }

        static bool CheckPathsOk(string[] paths)
        {
            foreach (var inPath in paths)
            {
                var fi = new FileInfo(inPath);
                if (fi.Attributes == FileAttributes.Directory)
                {
                    Console.Error.WriteLine($"Error: Is a directory: \"{inPath}\"");
                    return false;
                }
                if (!fi.Exists)
                {
                    Console.Error.WriteLine($"File not found: \"{inPath}\"");
                    return false;
                }
            }
            return true;
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
