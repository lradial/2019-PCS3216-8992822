using System;
using System.Collections.Generic;
using Projeto.VM;
using System.IO;

namespace Projeto.TwoPassAssembler
{
    public class Assembler
    {
        private Dictionary<string, Instruction> Mnemonics { get; } = new Dictionary<string, Instruction>
        {
            {"JP", Instruction.JP },
            {"JZ", Instruction.JZ },
            {"JN", Instruction.JN },
            {"CN", Instruction.CN },
            {"+", Instruction.ADD },
            {"-", Instruction.SUB },
            {"*", Instruction.MULT },
            {"/", Instruction.DIV },
            {"LD", Instruction.LD },
            {"MM", Instruction.MM },
            {"SC", Instruction.SC },
            {"OS", Instruction.OS },
            {"IO", Instruction.IO }
        };
        private Dictionary<string, PseudoInstruction> PseudoInstructions { get; }
        private ushort InstructionCounter { get; set; } = 0;
        private byte ByteCounter { get; set; } = 0;
        private List<byte> Buffer { get; } = new List<byte>();
        private byte Checksum { get; set; } = 0;
        private Dictionary<string, ushort> Labels { get; } = new Dictionary<string, ushort>();
        public Assembler()
        {
            PseudoInstructions = new Dictionary<string, PseudoInstruction>();
            foreach (var pseudo in PseudoInstruction.GetList())
            {
                PseudoInstructions.Add(pseudo.Mnemonic, pseudo);
            }
        }

        private void OutputToBuffer(byte B)
        {
            Buffer.Add(B);
            Checksum = (byte) ((Checksum + B) % 0x100);
        }

        private void OutputToBuffer(ushort W)
        {
            byte x0 = (byte)(W >> 8);
            byte x1 = (byte)W;
            OutputToBuffer(x0);
            OutputToBuffer(x1);
        }

        public bool Assembly(FileInfo source, FileInfo destination)
        {
            try
            {
                if (!source.Exists)
                {
                    Console.WriteLine($"Source file {source.Name} does not exist!");
                    return false;
                }
                Labels.Clear();
                InstructionCounter = 0;
                ByteCounter = 0;
                Checksum = 0;
                Buffer.Clear();
                if (!FirstPass(source))
                {
                    Console.WriteLine("First pass failed!");
                    return false;
                }
                if (!SecondPass(source))
                {
                    Console.WriteLine("Second pass failed!");
                    return false;
                }
                using (StreamWriter sw = new StreamWriter(destination.FullName))
                {
                    for (int i = 0; i < Buffer.Count; i++)
                    {
                        sw.Write($"{Buffer[i]:X2} ");
                    }
                }
                Console.WriteLine($"Assembly complete! \nOutput file: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{destination.FullName}");
                Console.ResetColor();
                return true;
            }
            catch (Exception e) when ((e is IOException) || (e is UnauthorizedAccessException))
            {
                Console.WriteLine($"Assembly failed! Error message:\n {e.Message}");
                return false;
            }
        }

        private bool FirstPass(FileInfo source)
        {
            using (StreamReader sr = new StreamReader(source.FullName))
            {
                int lineCounter = 0; // debug source file
                string line;
                string[] split;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().ToUpper();
                    split = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries); // ignore whitespace
                    // Instruction lines must begin with whitespace.
                    if (!char.IsWhiteSpace(line[0]))
                    {
                        // Must be a symbol/label
                        string newLabel = split[0];
                        if (PseudoInstructions.ContainsKey(newLabel) || Mnemonics.ContainsKey(newLabel))
                        {
                            Console.WriteLine($"Line #{lineCounter}: {line} is not valid!\nCannot declare a symbol with the same name as an instruction or pseudo-instruction.");
                            return false;
                        }
                        if (split.Length > 1)
                        {
                            // Declaring a new symbol.
                            if (split[1] == PseudoInstruction.Constant.Mnemonic)
                            {
                                Labels.Add(newLabel, InstructionCounter);
                                InstructionCounter += 1;
                                ByteCounter += 1;
                            }
                            else
                            {
                                Console.WriteLine($"Line #{lineCounter}: {line} is not valid!\nInvalid pseudo-intruction.");
                                return false;
                            }
                        }
                        else
                        {
                            // Declaring a new label.
                            Labels.Add(newLabel, InstructionCounter);
                        }
                    }
                    else
                    {
                        string instruction = split[0];
                        string operand = split[1];
                        if (instruction == PseudoInstruction.Origin.Mnemonic)
                        {
                            InstructionCounter = Utilities.StringToAddress(operand);
                        }
                        else if (instruction == PseudoInstruction.End.Mnemonic)
                        {
                            // should be eof
                        }
                        else if (Mnemonics.ContainsKey(instruction))
                        {
                            InstructionCounter += Mnemonics[instruction].Size;
                            ByteCounter += Mnemonics[instruction].Size;
                        }
                        else
                        {
                            Console.WriteLine($"Line #{lineCounter}: {line} is not valid!\nInvalid instruction.");
                            return false;
                        }
                    }
                    lineCounter++;
                }
            }
            return true;
        }

        private bool SecondPass(FileInfo source)
        {
            // For debugging purposes
            int lineCounter = 0;
            using (StreamReader sr = new StreamReader(source.FullName))
            {
                string line = sr.ReadLine().ToUpper();
                string[] split = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries); // ignore whitespace
                if (split[0] != PseudoInstruction.Origin.Mnemonic)
                {
                    Console.WriteLine($"Line #{lineCounter}: {line} is not valid!\nProgram must begin with origin declaration.");
                    return false;
                }
                else
                {
                    ushort address = Utilities.StringToAddress(split[1]);
                    InstructionCounter = address;
                    OutputToBuffer(address);
                    OutputToBuffer(ByteCounter);
                }
                while (!sr.EndOfStream)
                {
                    lineCounter++;
                    line = sr.ReadLine().ToUpper();
                    split = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries); // ignore whitespace
                    if (Labels.ContainsKey(split[0]) && split.Length > 1)
                    {
                        if (split[1] == PseudoInstruction.Constant.Mnemonic)
                        {
                            byte value = Utilities.StringToByte(split[2]);
                            OutputToBuffer(value);
                        }
                        else
                        {
                            Console.WriteLine($"Line #{lineCounter}: {line} is not valid!\nExpecting {PseudoInstruction.Constant.Mnemonic}");
                            return false;
                        }
                    }
                    else if (!PseudoInstructions.ContainsKey(split[0]) && split.Length > 1)
                    {
                        // Machine instruction
                        string operation = split[0];
                        string operand = split[1];
                        ushort opValue;
                        if (Labels.ContainsKey(operand))
                        {
                            opValue = Labels[operand];
                        }
                        else
                        {
                            opValue = Utilities.StringToAddress(operand);
                        }
                        if (Mnemonics[operation].Size == 1)
                        {
                            byte value = (byte)((Mnemonics[operation].Opcode >> 8 ) | (byte)opValue);
                            OutputToBuffer(value);
                        }
                        else
                        {
                            ushort value = (ushort)(Mnemonics[operation].Opcode | opValue);
                            OutputToBuffer(value);
                        }
                    }
                }
            }
            OutputToBuffer(Checksum);
            return true;
        }
    }
}