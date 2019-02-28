using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
// optional features: use, csect, equ
namespace SICXEAssembler
{
    class Assembler
    {
        public static bool TryAssemble(Program prog, out Binary result)
        {
            var inst = new Assembler(prog);
            if (!inst.PassOne())
            {
                Console.Error.WriteLine("Pass one failed.");
                result = null;
                return false;
            }
            Console.Error.WriteLine("Pass one succeeded.");

            if (!inst.PassTwo())
            {
                Console.Error.WriteLine("Pass two failed.");
                result = null;
                return false;
            }
            Console.Error.WriteLine("Pass two succeeded.");

            result = inst.Output;
            return true;
        }

        Program prog;

        public Binary Output
        { get; private set; }

        public Assembler(Program p)
        {
            prog = p;
            Output = new Binary();
        }

        /// <summary>
        /// This points to somewhere in 'codeSegment'.
        /// </summary>
        Symbol entryPoint = null;

        bool hitEnd = false; // We allow only one end directive.

        public int? BaseAddress => startAddress;
        int? startAddress;
        int? firstInstructionAddress; // The address of the first instruction in the program. Used to set the base address of the code segment.

        #region Symbols
        Dictionary<string, Symbol> symbols;

        public void PrintSymbolTable()
        {
            if (symbols == null)
                return;

            Console.WriteLine($"The symbol table contains {symbols.Count} {English.Pluralize(symbols.Count, "entry")}.");
            int start;
            if (startAddress.HasValue)
            {
                start = startAddress.Value;
                Console.WriteLine("Name\t\tAddress");
                Console.WriteLine("----\t\t-------");
            }
            else
            {
                start = 0;
                Console.WriteLine("Name\t\tAddress (Relative)");
                Console.WriteLine("----\t\t------------------");
            }

            foreach (var sym in symbols.Values)
            {
                if (sym.Address.HasValue)
                {
                    Console.WriteLine($"{sym.Name}\t\t{(start + sym.Address.Value).ToString("X6")}");
                }
                else
                {
                    Console.WriteLine($"{sym.Name}\t\t<not set>");
                }

            }
            Console.WriteLine();
        }

        /// <summary>
        /// Creates the symbol with the specified name if it does not exist.
        /// </summary>
        /// <param name="name"></param>
        private void TouchSymbol(string name)
        {
            if (!symbols.ContainsKey(name))
            {
                Symbol newSymbol;
                if (Literal.StringIsLiteralName(name))
                {
                    newSymbol = new Literal(name);
                    //symbols.Add(name, new Literal(name));
                }
                else
                {
                    newSymbol = new Symbol(name);
                    //symbols.Add(name, new Symbol(name));
                }
                symbols.Add(name, newSymbol);
                if (exports.ContainsKey(name))
                    exports[name] = newSymbol;
            }
        }

        /// <summary>
        /// Creates a symbol, ensuring it does not already exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the symbol was created. False if it already exists.</returns>
        private bool CreateSymbol(string name)
        {
            if (symbols.ContainsKey(name))
            {
                // The symbol already exists. Let caller display error message.
                return false;
            }
            var newSymbol = new Symbol(name);
            symbols[name] = newSymbol;
            if (exports.ContainsKey(name))
                exports[name] = newSymbol;
            return true;
        }

        /// <summary>
        /// Sets a symbol's value, ensuring it does not already have one. If the symbol with the specified name does not exist, it will be created.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="address"></param>
        /// <returns>True on success. False if the symbol already has a value assigned.</returns>
        private bool SetSymbolAddress(string name, int address, bool update = false)
        {
            Symbol existing;
            if (symbols.TryGetValue(name, out existing))
            {
                if (!update && existing.Address.HasValue)
                {
                    // The symbol already has value assigned. Let caller display error message.
                    return false;
                }
                existing.Address = address;
                return true;
            }
            // The symbol does not exist. We create it and assign it a value at the same time.
            var newSymbol = new Symbol(name) { Address = address };
            symbols.Add(name, newSymbol);
            if (exports.ContainsKey(name))
                exports[name] = newSymbol;
            return true;
        }
        #endregion

        private Dictionary<string, Symbol> exports;
        public IReadOnlyDictionary<string, Symbol> Exports => exports;


        // Call this in main method. all assemblers in job can share same instance.
        // main can detect and report name collisions.
        public void GiveImports(IReadOnlyDictionary<string, ExportedSymbol> importSource)
        {
            // Use each imported symbol wherever it is needed.
            foreach (var instr in prog.OfType<Instruction>())
            {
                foreach (var operand in instr.Operands)
                    UseImport(operand, importSource);
            }
        }

        private void UseImport(Operand o, IReadOnlyDictionary<string, ExportedSymbol> importSource)
        {
            if (o.Value.HasValue)
                return;
            if (importSource.TryGetValue(o.SymbolName, out ExportedSymbol es))
            {
                o.Value = es.Symbol.Address + es.ModuleBaseAddress;
                o.IsStartRelative = true;
            }
        }

        bool donePassOne = false;
        /// <summary>
        /// Takes account of all symbols declared or referenced, and computes the total length of the assembled binary.
        /// </summary>
        /// <param name="lstPath">The path of the file to which a listing of the partially assembled program will be written. If the file does not exist, it will be created. If null, no file is written.</param>
        /// <returns>True if assembly can continue. False on failure.</returns>
        public bool PassOne(string lstPath = null)
        {
            if (donePassOne)
            {
                // This indicates a bug.
                throw new InvalidOperationException("Pass one has already been done!");
            }
            //Console.Error.WriteLine("Beginning assembly pass one...\n");
            PreprocessLiterals();
            StreamWriter writer = null;
            if (lstPath != null)
            {
                writer = new StreamWriter(lstPath, false);
                writer.WriteLine($"Geoff's SIC/XE Assembler (built on {_BUILD_DATE.ToShortDateString()})");
                var now = DateTime.Now;
                writer.WriteLine($"Username: {Environment.UserName}; {now.ToShortDateString()} {now.ToLongTimeString()}");
                writer.WriteLine("-------------------------------------------------");
                writer.WriteLine("Assembler First Pass Report");
                writer.WriteLine("---------------------------");
                writer.WriteLine("Line   Address\tSource");
                writer.WriteLine("----   -------\t---------------------------------");
                Console.Error.WriteLine("Line   Address\tSource");
                Console.Error.WriteLine("----   -------\t---------------------------------");
            }

            int bytesSoFar = 0; // The total number of bytes in the assembled program.
            int instructionBytes = 0; // The total number of instruction bytes in the program.
            symbols = new Dictionary<string, Symbol>();
            exports = new Dictionary<string, Symbol>();
            for (int lineIdx = 0; lineIdx < prog.Count; ++lineIdx)
            {
                Line line = prog[lineIdx];
                //if (line.SkipPassOne)
                //continue;
                string label;
                if (line is AssemblerDirective dir)
                {
                    int val;
                    switch (dir.Directive)
                    {
                        case AssemblerDirective.Mnemonic.BYTE:
                            if (dir.Value == null)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Incomplete BYTE declaration is not allowed: {line.ToString()}");
                                return false;
                            }

                            var byteRegex = new Regex("([xc])'(.+)'", RegexOptions.IgnoreCase);
                            var match = byteRegex.Match(dir.Value);
                            if (!match.Success)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Cannot parse argument to BYTE directive.");
                                return false;
                            }

                            line.Address = bytesSoFar;
                            label = line.Label;
                            if (label != null)
                            {
                                if (!SetSymbolAddress(label, bytesSoFar, line.FromLiteral))
                                {
                                    Console.Error.WriteLine($"Error: Line {line.LineNumber}: Error: Multiple definitions of symbol \"{label}\"");
                                    return false;
                                }
                            }

                            int dataLength = match.Groups[2].Value.Length;
                            switch (match.Groups[1].Value[0])
                            {
                                case 'x':
                                case 'X':
                                    if (dataLength % 2 != 0)
                                    {
                                        //Console.Error.WriteLine($"Warning: Line {line.LineNumber}: Hex string has uneven number of characters. The left will be padded with 0.");
                                    }
                                    // For a hex literal, each pair of characters is a byte.
                                    bytesSoFar += (int)Math.Ceiling(dataLength / 2d);
                                    break;
                                case 'c':
                                case 'C':
                                    // For a character literal, each character is one byte.
                                    bytesSoFar += dataLength;
                                    break;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.WORD:
                            if (dir.Value == null)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Incomplete WORD declaration is not allowed: {line.ToString()}");
                                return false;
                            }
                            if (int.TryParse(dir.Value, out val))
                            {
                                label = line.Label;
                                if (label != null)
                                {
                                    if (!SetSymbolAddress(label, bytesSoFar))
                                    {
                                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Error: Multiple definitions of symbol \"{line.Label}\"");
                                        return false;
                                    }
                                    //symbols[label].Address = bytesSoFar;
                                }

                                line.Address = bytesSoFar;
                                bytesSoFar += Word.Size;
                            }
                            else
                            {
                                // todo: some word directives don't have the form of an integer. handle these.
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Could not parse word \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.RESW:
                        case AssemblerDirective.Mnemonic.RESB:
                            if (dir.Value == null)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Incomplete RESW or RESB declaration is not allowed: {line.ToString()}");
                                return false;
                            }

                            if (int.TryParse(dir.Value, out val))
                            {
                                label = line.Label;
                                if (label != null)
                                {
                                    if (!SetSymbolAddress(label, bytesSoFar))
                                    {
                                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Error: Multiple definitions of symbol \"{line.Label}\"");
                                        return false;
                                    }
                                }

                                line.Address = bytesSoFar;
                                if (dir.Directive == AssemblerDirective.Mnemonic.RESW)
                                {
                                    bytesSoFar += Word.Size * val;
                                }
                                else
                                {
                                    bytesSoFar += val;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Could not parse integer \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.START:
                            if (hitEnd)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Assembler directive \"{dir.ToString()}\" cannot appear after END.");
                                return false;
                            }
                            if (startAddress.HasValue)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple START directives are not allowed.");
                                return false;
                            }

                            if (dir.Value == null)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: START directive must be followed by an address!");
                                return false;
                            }
                            if (int.TryParse(dir.Value, System.Globalization.NumberStyles.HexNumber, null, out val))
                            {
                                startAddress = val;
                                line.Address = 0; // by definition. That is, this is the offset relative to START.
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Cannot parse start address \"{dir.Value}\".");
                                return false;
                            }
                            label = dir.Label;
                            if (label != null)
                            {
                                if (!SetSymbolAddress(label, bytesSoFar))
                                {
                                    Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple definitions of symbol \"{line.Label}\"");
                                    return false;
                                }
                            }
                            break;
                        case AssemblerDirective.Mnemonic.END:
                            if (hitEnd)
                            {
                                Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple END directives are not allowed.");
                                return false;
                            }
                            hitEnd = true;

                            if (dir.Value == null)
                            {
                                Console.Error.WriteLine($"Warning: Line {line.LineNumber}: Empty END directive.");
                            }
                            else
                            {
                                if (int.TryParse(dir.Value, out val))
                                {
                                    if (line.Label != null)
                                        if (!CreateSymbol(line.Label))
                                        {
                                            Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple declarations of symbol \"{line.Label}\"");
                                            return false;
                                        }
                                }
                                else
                                {
                                    TouchSymbol(dir.Value);
                                    entryPoint = symbols[dir.Value];
                                }
                            }

                            line.Address = bytesSoFar;
                            break;
                        case AssemblerDirective.Mnemonic.LTORG:
                            line.Address = bytesSoFar;
                            var ltorg = (LTORG)line;
                            // Calculate number of bytes that will go here for informational purposes.
                            // PreprocessLiterals has already transformed all literals into BYTE directives,
                            // so there's no need to grow the binary due to LTORG at this point.
                            int literalBytesSoFar = 0;
                            foreach (var sym in symbols)
                            {
                                Literal lit = sym.Value as Literal;
                                if (lit != null)
                                {
                                    // todo: verify that LTORG operates on only the literals that haven't already been assembled.
                                    lit.Address = bytesSoFar + literalBytesSoFar;
                                    literalBytesSoFar += lit.Data.Length;
                                    ltorg.Literals.Add(lit);
                                }
                            }
                            Console.Error.WriteLine($"Info: Line {line.LineNumber}: {literalBytesSoFar} bytes of literals pertain to LTORG at 0x{line.Address.Value.ToString("X")}.");
                            break;
                        case AssemblerDirective.Mnemonic.BASE:
                            line.Address = bytesSoFar;
                            label = line.Label;
                            if (label != null)
                            {
                                if (!SetSymbolAddress(label, bytesSoFar))
                                {
                                    Console.Error.WriteLine($"Error: Line {line.LineNumber}: Error: Multiple definitions of symbol \"{line.Label}\"");
                                    return false;
                                }
                            }

                            TouchSymbol(dir.Value);

                            break;
                    } // switch (dir.Directive).

                    if (!startAddress.HasValue)
                    {
                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Assembler directive \"{dir.ToString()}\" cannot appear before START.");
                        return false;
                    }
                }
                else if (line is Instruction)
                {
                    if (!startAddress.HasValue)
                    {
                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Code cannot appear before START directive.");
                        return false;
                    }
                    if (hitEnd)
                    {
                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Code cannot appear after END.");
                        return false;
                    }

                    if (!firstInstructionAddress.HasValue)
                        firstInstructionAddress = bytesSoFar + startAddress;

                    // Ensure label is in the symbol table.
                    label = line.Label;
                    if (label != null)
                    {
                        TouchSymbol(label);
                        if (symbols[label].Address != null)
                        //if (!CreateSymbol(label))
                        {
                            Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple declarations of symbol \"{label}\"");
                            return false;
                        }
                        symbols[label].Address = bytesSoFar;
                    }

                    // If operand is a symbol, ensure we include it in the symbol table.
                    var instr = (Instruction)line;
                    if ((instr.Format == InstructionFormat.Format3 || instr.Format == InstructionFormat.Format4) && instr.Operands.Count > 0)
                    {
                        var sym = instr.Operands[0].SymbolName;
                        if (sym != null)
                        {
                            sym = TrimIndexer(sym);
                            TouchSymbol(sym);
                        }
                    }

                    // Set address.
                    line.Address = bytesSoFar;
                    bytesSoFar += (int)instr.Format;
                    instructionBytes += (int)instr.Format;
                }
                else if (line is ExportDirective)
                {
                    var expLabel = line.Label;
                    if (exports.ContainsKey(expLabel))
                    {
                        Console.Error.WriteLine($"Error: Line {line.LineNumber}: Multiple export directives for \"{expLabel}\".");
                        return false;
                    }
                    else
                    {
                        exports[expLabel] = null; // To be set later this pass, once we encounter a definition for the symbol.
                    }
                }

                if (writer != null)
                {
                    int separation = prog.LongestLabel;
                    if (separation < 1)
                        separation = 1;
                    string address = line.Address.HasValue ? (startAddress.Value + line.Address.Value).ToString("X6") : "??????";
                    string printedLine;
                    if (line.Comment != null && line.Comment.Length > 0)
                        printedLine = $"{line.LineNumber.ToString("D3")}    {address}\t{line.ToString(separation)}    \t{line.Comment}";
                    else
                        printedLine = $"{line.LineNumber.ToString("D3")}    {address}\t{line.ToString(separation)}";
                    Console.WriteLine(printedLine);
                    writer.WriteLine(printedLine);

                }
            } // For each line.

            if (writer != null)
            {
                writer.Dispose();
            }

            donePassOne = true;
            return true;
        }

        private void PreprocessLiterals()
        {
            var newLiterals = new List<Literal>();
            for (int i = 0; i < prog.Count; ++i)
            {
                var line = prog[i];
                var instr = line as Instruction;
                if (instr != null)
                {
                    foreach (var operand in instr.Operands)
                    {
                        var symName = operand.SymbolName;
                        if (symName != null && Literal.StringIsLiteralName(symName))
                        {
                            newLiterals.Add(new Literal(symName));
                        }
                    }
                }
                else
                {
                    var dir = line as AssemblerDirective;
                    if (dir == null)
                    {
                        if (line is ImportDirective || line is ExportDirective)
                            continue;
#if DEBUG

                        throw new InvalidOperationException("Unrecognized line type!?");
#else
                        continue; // Ignore this line.
#endif
                    }
                    if (dir.Directive == AssemblerDirective.Mnemonic.LTORG)
                    {
                        int litOffset = 1;
                        foreach (var lit in newLiterals)
                        {
                            var newDir = new AssemblerDirective(AssemblerDirective.Mnemonic.BYTE);
                            newDir.Comment = "Generated by assembler";
                            newDir.Label = lit.Name;
                            newDir.Value = lit.Name.Substring(1); // Trim leading '=' to get BYTE argument.
                            newDir.FromLiteral = true; // Tell pass one to ignore this.
                            Debug.WriteLine($"LTORG: Adding new line {newDir.ToString()}");
                            prog.Insert(i + litOffset++, newDir);
                        }
                        newLiterals.Clear();
                    }
                }
            }
            // Take care of any remaining literals (implicit final LTORG).
            foreach (var lit in newLiterals)
            {
                var newDir = new AssemblerDirective(AssemblerDirective.Mnemonic.BYTE);
                newDir.Comment = "Generated by assembler";
                newDir.Label = lit.Name;
                newDir.Value = lit.Name.Substring(1); // Trim leading '=' to get BYTE argument.
                newDir.FromLiteral = true; // Tell pass one to ignore this.
                prog.Insert(prog.Count - 1, newDir);
            }

        }

        bool donePassTwo = false;
        public bool PassTwo(string lstPath = null)
        {
            if (donePassTwo)
            {
                // This indicates a bug.
                throw new InvalidOperationException("Pass two has already been done!");
            }
            //Console.Error.WriteLine("Beginning assembly pass two...\n");

            // Attempt to satisfy our imports.
            // We do this by searching for symbols (in our own symbol table) that still lack an address.
            // For each such symbol, check 'Imports'.

            StreamWriter writer = null;
            if (lstPath != null)
            {
                writer = new StreamWriter(lstPath, false);
                writer.WriteLine($"Geoff's SIC/XE Assembler (built on {_BUILD_DATE.ToShortDateString()})");
                var now = DateTime.Now;
                writer.WriteLine($"Username: {Environment.UserName}; {now.ToShortDateString()} {now.ToLongTimeString()}");
                writer.WriteLine("-------------------------------------------------");
                writer.WriteLine("Assembler First Pass Report");
                writer.WriteLine("---------------------------");
                writer.WriteLine("Line   Address\tBinary\tSource");
                writer.WriteLine("----   -------\t------\t---------------------------------");
            }

            int currentSegmentStart = startAddress.Value;
            Segment currentSegment = new Segment
            {
                BaseAddress = currentSegmentStart,
                Data = new List<byte>(),
                OriginFile = prog.OriginFile
            };
            Output.AddSegment(currentSegment);

            int segmentIndex = 0; // Index in the code segment.
            int overallIndex = 0;
            byte[] binInstr = null;
            int? @base = null; // for base directive.
            byte[] lineBytesToDisplay = null; // For for printing to LST file/console.
            for (int lineIdx = 0; lineIdx < prog.Count; ++lineIdx)
            {
                Line line = prog[lineIdx];
                if (line is Instruction instr)
                {
                    Debug.Assert(instr.Address == overallIndex);

                    // Set each operand symbol's address using the symbol table.
                    for (int operandIdx = 0; operandIdx < instr.Operands.Count; ++operandIdx)
                    {
                        Operand operand = instr.Operands[operandIdx];
                        if (!operand.Value.HasValue)
                        {
                            string sym = TrimIndexer(operand.SymbolName);
                            Debug.Assert(sym != null);
                            operand.Value = symbols[sym].Address;
                        }
                    }
                    var operands = instr.Operands;

                    switch (instr.Format)
                    {
                        case InstructionFormat.Format1:
                            if (operands.Count != 0)
                            {
                                ReportError($"Format 1 instruction takes no operands, but \"{string.Join(", ", operands)}\" was given!", instr);
                                return false;
                            }
                            binInstr = new byte[] { (byte)instr.Operation };
                            break;
                        case InstructionFormat.Format2:
                            if (!(operands.Count == 1 || operands.Count == 2))
                            {
                                ReportError($"Format 2 instruction takes 1 or 2 operands, but \"{string.Join(", ", operands)}\" was given!", instr);
                                return false;
                            }
                            binInstr = AssembleFormat2(instr);
                            break;
                        case InstructionFormat.Format3:
                        case InstructionFormat.Format4:
                            try
                            {
                                binInstr = AssembleFormats34(instr,
                                    startAddress.Value,
                                    instr.Address.Value + (int)instr.Format,
                                    @base);
                            }
                            catch (ArgumentException ex)
                            {
                                ReportError(ex.Message, instr);
                                return false;
                            }
                            break;
                        default:
                            // This indicates a bug.
#if DEBUG
                            throw new ArgumentException($"Instruction has a bad format.");
#else
                        ReportError("Instruction has a bad format.", instr);
                        break;
#endif
                    }
                    currentSegment.Data.AddRange(binInstr);
                    segmentIndex += binInstr.Length;
                    overallIndex += binInstr.Length;
                    lineBytesToDisplay = binInstr;
                }
                else if (line is AssemblerDirective dir)
                {
                    Debug.Assert(dir.Address == segmentIndex);
                    byte[] buf;
                    switch (dir.Directive)
                    {
                        case AssemblerDirective.Mnemonic.START:
                            // Don't do anything.
                            lineBytesToDisplay = null;
                            break;
                        case AssemblerDirective.Mnemonic.END:
                            // Don't do anything.
                            // In pass one, Symbol this.entryPoint should have already been set.
                            lineBytesToDisplay = null;
                            break;
                        case AssemblerDirective.Mnemonic.BASE:
                            Symbol baseSymbol;
                            if (symbols.TryGetValue(dir.Value, out baseSymbol) && baseSymbol.Address.HasValue)
                            {
                                @base = baseSymbol.Address.Value;
                            }
                            else
                            {
                                ReportError($"Undefined symbol \"{baseSymbol.Name}\".", dir);
                                return false;
                            }
                            lineBytesToDisplay = null;
                            break;
                        case AssemblerDirective.Mnemonic.BYTE:
                            try
                            {
                                buf = AssembleByteDirective(dir);
                            }
                            catch (Exception ex)
                            {
                                if (!(ex is ArgumentException || ex is FormatException))
                                    throw;
                                ReportError($"Could not parse argument to byte directive.", dir);
                                return false;
                            }
                            currentSegment.Data.AddRange(buf);
                            segmentIndex += buf.Length;
                            overallIndex += buf.Length;
                            lineBytesToDisplay = buf;
                            break;
                        case AssemblerDirective.Mnemonic.WORD:
                            Word parsed;
                            if (!Word.TryParse(dir.Value, out parsed))
                            {
                                ReportError($"Could not parse word \"{dir.Value}\".", dir);
                                return false;
                            }
                            buf = parsed.ToArray().Reverse().ToArray();
                            Debug.Assert(buf.Length == Word.Size);
                            currentSegment.Data.AddRange(buf);
                            segmentIndex += buf.Length;
                            overallIndex += buf.Length;
                            lineBytesToDisplay = buf.ToArray();
                            break;
                        case AssemblerDirective.Mnemonic.RESB:
                        case AssemblerDirective.Mnemonic.RESW:
                            int UNIT;
                            if (dir.Directive == AssemblerDirective.Mnemonic.RESW)
                                UNIT = Word.Size; // RESW 5 means reserve 15 bytes.
                            else
                                UNIT = 1; // RESB 5 means reserve 5 bytes.
                            int size;
                            if (int.TryParse(dir.Value, out size))
                            {
                                Output.AddSegment(currentSegment); // Push current segment.

                                var newSeg = new Segment(); // Begin new segment.
                                newSeg.OriginFile = prog.OriginFile;
                                segmentIndex += size * UNIT;
                                overallIndex += size * UNIT;
                                newSeg.BaseAddress = startAddress.Value + segmentIndex;
                                newSeg.Data = new List<byte>();
                                Output.AddSegment(newSeg);
                                currentSegment = newSeg;
                            }
                            else
                            {
                                ReportError($"Could not parse \"{dir.Value}\". Only integers are supported in {dir.Directive.ToString()} directive.", dir);
                                return false;
                            }
                            lineBytesToDisplay = null;
                            break;
                        case AssemblerDirective.Mnemonic.LTORG:
                            break; // this logic is already handled elsewhere; no need to do anything now.
                        case AssemblerDirective.Mnemonic.EQU:

                            break;
                        default:
                            // This indicates a bug.
#if DEBUG
                            throw new ArgumentException($"Unrecognized assembler directive.");
#else
                            ReportError("Ignoring unrecognized assembler directive.", instr);
                            break;
#endif
                    }
                }
                if (writer != null)
                {
                    int separation = prog.LongestLabel;
                    if (separation < 1)
                        separation = 1;
                    string address = line.Address.HasValue ? (startAddress.Value + line.Address.Value).ToString("X6") : "??????";
                    string printedLine;
                    if (line.Comment != null && line.Comment.Length > 0)
                    {
                        if (lineBytesToDisplay != null)
                        {
                            printedLine = $"{line.LineNumber.ToString("D3")}    {address}\t{string.Join("", lineBytesToDisplay.Select(b => b.ToString("X2"))).PadRight(10)}\t{line.ToString(separation)}    \t{line.Comment}";
                        }
                        else
                        {
                            printedLine = $"{line.LineNumber.ToString("D3")}    {address}        \t\t{line.ToString(separation)}    \t{line.Comment}";
                        }
                    }
                    else
                    {
                        if (lineBytesToDisplay != null)
                        {
                            printedLine = $"{line.LineNumber.ToString("D3")}    {address}\t{string.Join("", lineBytesToDisplay.Select(b => b.ToString("X2"))).PadRight(10)}\t{line.ToString(separation)}";
                        }
                        else
                        {
                            printedLine = $"{line.LineNumber.ToString("D3")}    {address}        \t\t{line.ToString(separation)}";
                        }
                    }
#if DEBUG
                    Console.Error.WriteLine(printedLine);
#endif
                    writer.WriteLine(printedLine);
                }
            }

            if (entryPoint != null)
            {
                if (entryPoint.Address.HasValue)
                {
                    Output.EntryPoint = entryPoint.Address.Value + Output.Segments.First().BaseAddress.Value;
                }
                else
                {
                    ReportError($"Symbol \"{entryPoint.Name}\" (in END directive) is undefined!", null);
                }
            }
            else
            {
                int ep = Output.Segments.First().BaseAddress.Value;
                Console.Error.WriteLine($"Warning: Missing or incomplete END directive. Assuming entry point is {ep.ToString("X")}.");
                Output.EntryPoint = ep;
            }

            if (writer != null)
            {
                writer.Dispose();
            }

            donePassTwo = true;
            return true;
        } // PassTwo().

        // Called during pass two.
        private byte[] AssembleByteDirective(AssemblerDirective dir)
        {
            if (dir.Directive != AssemblerDirective.Mnemonic.BYTE)
                throw new ArgumentException("Directive must be of BYTE type to be processed by this method.");

            string str = dir.Value;
            char byteType = dir.Value[0];
            string payload = dir.Value.Substring(2, str.Length - 3);
            switch (byteType)
            {
                case 'c':
                case 'C':
                    return System.Text.Encoding.ASCII.GetBytes(payload);
                case 'x':
                case 'X':
                    if ((payload.Length & 1) > 0)
                    {
                        Console.Error.WriteLine($"Warning: Hex string in line {dir.LineNumber} contains uneven number of characters. The left will be padded with 0.");
                        payload = '0' + payload;
                    }
                    return Literal.GetBytesFromHexString(payload);
            }
            throw new ArgumentException(nameof(dir));
        }

        // Called during pass two (but could be called during pass one!)
        private byte[] AssembleFormat2(Instruction instr)
        {
            if (instr.Format != InstructionFormat.Format2)
                throw new ArgumentException("Instruction must be format 2 to be processed by this method.");

            byte[] ret = new byte[2];
            ret[0] = (byte)instr.Operation;
            ret[1] = (byte)(instr.Operands[0].Value << 4);
            if (instr.Operands.Count > 1)
                ret[1] |= (byte)(instr.Operands[1].Value);

            return ret;
        }

        // Called during pass two.
        private byte[] AssembleFormats34(Instruction instr, int programBaseAddress, int programCounter, int? baseRegister)
        {
            const int MAX_F4_DISP = 1 << 20;
            const int MIN_PC_DISP = -(1 << 11);
            const int MAX_PC_DISP = 1 << 11;
            const int MIN_BASE_DISP = 0;
            const int MAX_BASE_DISP = 1 << 12;
#if DEBUG
            //if (instr.Operation == Instruction.Mnemonic.J)
                //Debugger.Break();
#endif

            int oplen = (int)instr.Format;

            if (oplen != 3 && oplen != 4)
                throw new ArgumentException("Instruction must be format 3 or 4 to be processed by this method.");

            var binInstr = new byte[oplen];
            Array.Clear(binInstr, 0, oplen); // Initialize array to all zeroes. (Not sure if this is necessary.)
            binInstr[0] = (byte)instr.Operation; // Set first byte of instruction to opcode.

            int opcount = instr.Operands.Count;
            if (opcount == 1)
            {
                var firstOperand = instr.Operands[0];
                if (!firstOperand.Value.HasValue)
                    throw new ArgumentException($"Symbol \"{TrimIndexer(firstOperand.SymbolName)}\" is undefined!");

                // Set flags that don't require knowledge of the displacement, N I X.
                AddressingMode mode = firstOperand.AddressingMode;
                bool indirect = mode.HasFlag(AddressingMode.Indirect);
                bool immediate = mode.HasFlag(AddressingMode.Immediate);
                bool indexed = mode.HasFlag(AddressingMode.Indexed);
                if (!(indirect || immediate))
                {
                    binInstr[0] |= 3; // Set N,I flags.
                }
                else
                {
                    if (indirect)
                    {
                        binInstr[0] |= 2; // Set N flag.
                    }
                    if (immediate)
                    {
                        binInstr[0] |= 1; // Set I flag.
                    }
                }
                if (indexed)
                {
                    binInstr[1] |= 0x80; // Set X flag.
                }

                // Use extended addressing, if it is indicated.
                int disp = firstOperand.Value.Value;

                if (instr.Format == InstructionFormat.Format4)
                {
                    if (immediate)
                    {
                        if (disp < 0)
                            throw new ArgumentException("Displacement cannot be negative using extended, immediate addressing!");

                        if (disp > MAX_F4_DISP)
                            throw new ArgumentException($"Displacement is too large for extended mode: maximum is {MAX_F4_DISP}.");
                    }
                    else
                    {
                        disp += programBaseAddress;
                    }

                    // ni xbpe
                    // 21 8421
                    binInstr[1] |= 0x10; // Set E flag.
                    InsertDisplacement(binInstr, disp);
                    return binInstr;
                }

                if (immediate)
                {
                    if (disp >= MIN_PC_DISP && disp <= MAX_PC_DISP)
                    {
                        InsertDisplacement(binInstr, disp);
                        return binInstr;
                    }
                    else
                    {
                        throw new ArgumentException($"Immediate operand cannot fit in format 3 instruction: minimum is {MIN_PC_DISP}, maximum is {MAX_PC_DISP}.");
                    }
                }

                // Try using PC-relative addressing.
                
                //disp = programCounter - disp; // disp now represents the offset between the operand's value and the program counter.
                // todo: fix this.
                // sometimes a symbol is already startaddress-relative
                // other times it is not.

                if (firstOperand.IsStartRelative)
                    disp = disp - programCounter - startAddress.Value;
                else
                    disp = disp - programCounter;

                if (disp >= MIN_PC_DISP && disp <= MAX_PC_DISP)
                {
                    // PC-relative addressing is valid.
                    binInstr[1] |= 0x20; // Set P flag.
                    InsertDisplacement(binInstr, disp);
                    return binInstr;
                }

                // PC-relative addressing failed. Try base-relative addressing.
                // Base-relative addressing will "work as expected" at execution time only if the value of the base register matches the 'baseRegister' parameter of this method.
                if (baseRegister.HasValue)
                {
                    disp = firstOperand.Value.Value - baseRegister.Value;
                    if (disp >= MIN_BASE_DISP && disp <= MAX_BASE_DISP)
                    {
                        // Base-relative addressing is valid.
                        binInstr[1] |= 0x40; // Set B flag.
                        InsertDisplacement(binInstr, disp);
                        return binInstr;
                    }
                }

                throw new ArgumentException($"Could not assemble format 3 instruction using displacement 0x{firstOperand.Value.Value.ToString("X")}.");
            }

            if (opcount == 0)
            {
                // The only nullary format 3/4 instruction is RSUB.
                if (instr.Operation != Instruction.Mnemonic.RSUB)
                    throw new ArgumentException($"Missing operand(s) for instruction {instr.ToString()}.");

                // What should we expect addressing mode to be here?
                // all flags zero?
                // ni=11, rest zero?
                return new byte[] { (byte)Instruction.Mnemonic.RSUB, 0, 0 };
            }
            throw new ArgumentException($"Too many operands for format {oplen} instruction {instr.ToString()}.");
        }

        // Called during pass two.
        private static void InsertDisplacement(byte[] instruction, int displacement)
        {
            int len = instruction.Length;
            byte[] dispBytes;
            switch (len)
            {
                case 3:
                    dispBytes = EncodeNumber(displacement, 12);
                    Debug.Assert(dispBytes.Length == 2, "EncodeNumber gave us wrong number of bytes!");
                    Debug.Assert((instruction[1] & 0x3) == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[2] == 0, "disp bits are already set in instruction!");
                    instruction[2] = dispBytes[0];
                    instruction[1] |= dispBytes[1];
                    break;
                case 4:
                    dispBytes = EncodeNumber(displacement, 20);
                    Debug.Assert(dispBytes.Length == 3, "EncodeNumber gave us wrong number of bytes!");
                    Debug.Assert((instruction[1] & 0x3) == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[2] == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[3] == 0, "disp bits are already set in instruction!");
                    instruction[3] = dispBytes[0];
                    instruction[2] = dispBytes[1];
                    instruction[1] |= dispBytes[2];
                    break;
                default:
                    throw new ArgumentException("Instruction length must be 3 or 4 bytes in length.");
            }
        }

        /// <summary>
        /// Removes ",x" from the end of the string, if it is present.
        /// </summary>
        private static string TrimIndexer(string symbol)
        {
            const string INDEX_SUFFIX = ",x";
            if (symbol.EndsWith(INDEX_SUFFIX, StringComparison.InvariantCultureIgnoreCase))
            {
                return symbol.Substring(0, symbol.Length - INDEX_SUFFIX.Length);
            }
            return symbol;
        }

        private static byte[] EncodeNumber(int n, int bits)
        {
            int highMask = checked(~((1 << bits) - 1));
            if ((highMask & bits) > 0)
            {
                // Higher bits are set in 'n' than 2^bits.
                throw new OverflowException();
            }
            n &= ~highMask;

            int retlen = (int)Math.Ceiling(bits / 8d);
            var ret = new byte[retlen];
            for (int b = 0; b < retlen; ++b)
            {
                int only = (0xff << (b * 8)) & n;
                ret[b] = (byte)(only >> (b * 8));
            }
            return ret;
        }

        static readonly DateTime _BUILD_DATE;
        static Assembler()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            _BUILD_DATE = new DateTime(2000, 1, 1).Add(new TimeSpan(TimeSpan.TicksPerDay * version.Build + 2 * TimeSpan.TicksPerSecond * version.Revision));
        }

        private void ReportError(string message, Line line)
        {
            Console.Error.WriteLine($"\nError: {message}");
            if (line != null)
            {
                Console.Error.WriteLine($"Line {line.LineNumber}:\n\t{line.ToString()}");
            }
        }
    }
}
