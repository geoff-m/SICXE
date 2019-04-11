using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace SICXEAssembler
{
    public class Assembler
    {
        public Assembler()
        {
            files = new Dictionary<string, AssemblerJobPart>();
        }

        IDictionary<string, AssemblerJobPart> files;
        IDictionary<string, ExportedSymbol> exports;

        class AssemblerJobPart
        {
            public readonly string FilePath;
            public Program Program;
            public FileAssembler Assembler;
            public Binary Binary;
            public readonly bool IsEntryPoint;
            public AssemblerJobPart(string filePath, bool isEntryPoint = false)
            {
                FilePath = filePath;
                IsEntryPoint = isEntryPoint;
            }
        }

        private AssemblerJobPart EntryPoint = null;
        public void AddInputFile(string path, bool isEntryPoint = false)
        {
            AssertPathOk(path);
            var jp = new AssemblerJobPart(path, isEntryPoint);
            if (isEntryPoint)
            {
                if (EntryPoint != null)
                    throw new ArgumentException($"Entry point has already been set in \"{EntryPoint.FilePath}\"");
                EntryPoint = jp;
            }
            files.Add(path, jp);
        }

        private void ParseFiles()
        {
            try
            {
                foreach (var kvp in files)
                {
                    if (Program.TryParse(kvp.Key, out Program prog))
                    {
                        kvp.Value.Program = prog;
                    }
                    else
                    {
                        throw new InvalidProgramException($"Error parsing \"{kvp.Key}\".", null);
                    }
                }
            }
            catch (IOException ex)
            {
                throw new InvalidProgramException(ex.Message, null, ex);
            }
        }

        private void DoPassOnes()
        {
            string currentFile = null;
            try
            {
                foreach (var f in files.Keys)
                {
                    currentFile = f; // So that it's accessible in the catch block.
                    var assembler = new FileAssembler(files[f].Program);
                    files[f].Assembler = assembler;
                    //var lstPath = f + ".lst";
                    //var objPath = f + ".obj";
                    //Console.Error.WriteLine($"Assembling \"{f}\"...");
                    try
                    {
                        assembler.PassOne();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidProgramException($"Assembly pass one failed for {f}: {ex.Message}", null, ex);
                    }

                }
            }
            catch (Exception ex)
            {
                if (currentFile != null)
                {
                    if (ex is System.Threading.ThreadAbortException)
                    {
                        throw new InvalidProgramException($"Aborted during assembling \"{currentFile}\".", null);
                        throw; // To ensure abort rises even if IPE is caught.
                    }
                    else
                    {
                        throw new InvalidProgramException($"Error assembling \"{currentFile}\": {ex.Message}", null, ex);
                    }
                }
                else
                    throw new InvalidProgramException(ex.Message, null, ex);
            }
        }

        private void BuildExportTable()
        {
            exports = new Dictionary<string, ExportedSymbol>();
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
        }

        private void DoPassTwos()
        {
            foreach (var f in files.Keys)
            {
                var asm = files[f].Assembler;

                asm.GiveImports((IReadOnlyDictionary<string, ExportedSymbol>)exports); // Inform assembler of symbols exported by others.
                //try
                //{
                    asm.PassTwo(f + ".lst");
                    files[f].Binary = asm.Output;
                //}
                //catch (Exception ex)
                //{
                    //throw new InvalidProgramException($"Assembly pass two failed for {f}: {ex.Message}", null, ex);
                //}
            }
        }

        private static void AssertPathOk(string path)
        {
            var fi = new FileInfo(path);
            if (fi.Attributes == FileAttributes.Directory)
            {
                throw new ArgumentException($"Error: Is a directory: \"{path}\"");
            }
            if (!fi.Exists)
            {
                throw new ArgumentException($"File not found: \"{path}\"");
            }
        }

        public Binary AssembleBinary()
        {
            if (EntryPoint == null)
                throw new InvalidOperationException("No assembly has been chosen to specify the entry point!");

            // Parse all source files as Programs.
            ParseFiles();

            // Assemble each program.
            // We have to do this individually so that symbol names don't collide.
            // First do all pass ones. This discovers all symbol defs and refs.
            // Then we can satisfy imports before doing pass two.
            DoPassOnes();


            // Build table of all exported symbols.
            BuildExportTable();


            // Do assembly pass two.
            DoPassTwos();

            // Attempt to combine the binaries.

            // First check for overlap.
            CheckForOverlap(); // throws if overlap is found.

            // We got this far because they didn't overlap.
            // Combine the binaries.
            return CombineBinaries();
        }

        public void PrintExportTable()
        {
            Console.WriteLine("Public symbols\n-----------------------------------");
            ExportedSymbol.PrintTable(exports.Values.ToList());
        }

        private void CheckForOverlap()
        {
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
                    var message = $"Error: Code or data in {f} overlaps code or data in {g}.";
                    if (overlap != null)
                    {
                        var frange = fbin.GetMaximumRange();
                        var grange = gbin.GetMaximumRange();
                        message += $" {Path.GetFileName(f)} starts at 0x{frange.Start:X6} and ends at 0x{frange.Stop:X6}.";
                        message += $" {Path.GetFileName(g)} starts at 0x{grange.Start:X6} and ends at 0x{grange.Stop:X6}.";
                        return;
                    }
                    else
                    {
                        //Debug.Fail("CanCombineWithoutRelocating returned true but FindCollidingSegments returned null!");
                        // This can be caused by the collision happening where there's a RESW.
                        // CCWR will detect it but FCS will not because RESW does not actually create a segment.
                    }
                    throw new System.InvalidProgramException(message);
                }
            }
        }

        private Binary CombineBinaries()
        {
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

            bigBin.EntryPoint = EntryPoint.Binary.EntryPoint;
            return bigBin;
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
    }
}
