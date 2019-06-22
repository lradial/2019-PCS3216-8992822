using System;
using System.Collections.Generic;
using System.IO;

namespace Projeto.VM
{
    public class VirtualMachine
    {
        private bool IsDebug { get; }
        private Dictionary<byte, Instruction> Instructions { get; }
        public sbyte Accumulator { get; private set; } = 0;
        private Stack<ushort> CallStack { get; } = new Stack<ushort>();
        public Memory Memory { get; private set; } = new Memory();
        public ushort ProgramCounter { get; private set; } = 0;
        public bool IsRunning { get; private set; } = false;
        public bool Error { get; private set; } = false;
        private bool IsPointer { get; set; } = false;
        public FileInfo DeviceInput { get; }
        public FileInfo DeviceOutput { get; }

        public VirtualMachine(FileInfo input, FileInfo output, bool debug = false)
        {
            DeviceInput = input;
            DeviceOutput = output;
            IsDebug = debug;
            // Initialize Instructions dictionary
            Instructions = new Dictionary<byte, Instruction>();
            foreach (var instr in Instruction.GetList())
            {
                Instructions.Add((byte)(instr.Opcode >> 12), instr);
            }
        }

        public void Run(ushort address)
        {
            char[] buffer = new char[3];
            ushort operand = 0;
            byte currentByte = 0;
            Instruction currentInstruction;
            ProgramCounter = address;
            Error = false;
            IsRunning = true;
            ushort maxPC = ProgramCounter; // debug
            DebugMessage($"Running Program from {ProgramCounter:X4}");
            using (StreamReader sr = new StreamReader(DeviceInput.FullName))
            using (StreamWriter sw = new StreamWriter(DeviceOutput.FullName))
            {
                while (IsRunning)
                {
                    if (Error)
                    {
                        DebugMessage("An error occurred.");
                        return;
                    }
                    // Read a byte from memory
                    currentByte = Memory[ProgramCounter];
                    currentInstruction = Instructions[(byte)((currentByte & 0xf0) >> 4)];
                    if (currentInstruction.Size == 1)
                    {
                        DebugMessage($"Current Instruction: {currentByte:X2}");
                        if (currentInstruction == Instruction.IO)
                        {
                            IOCall(buffer, currentByte, sr, sw);
                        }
                        else if (currentInstruction == Instruction.CN)
                        {
                            Control((byte)(currentByte & 0x0f));
                        }
                        else // OS
                        {
                            OSCall((byte)(currentByte & 0x0f));
                        }
                    }
                    else
                    {
                        // 12 LSB are the operand
                        operand = (ushort)(((currentByte << 8) | Memory[++ProgramCounter]) & 0x0fff);
                        DebugMessage($"Current Instruction: {currentInstruction.Opcode | operand:X4}");
                        if (currentInstruction == Instruction.JP)
                        {
                            Jump(operand);
                        }
                        else if (currentInstruction == Instruction.JZ)
                        {
                            JumpZero(operand);
                        }
                        else if (currentInstruction == Instruction.JN)
                        {
                            JumpNegative(operand);
                        }
                        else if (currentInstruction == Instruction.ADD)
                        {
                            Add(operand);
                        }
                        else if (currentInstruction == Instruction.SUB)
                        {
                            Subtract(operand);
                        }
                        else if (currentInstruction == Instruction.MULT)
                        {
                            Multiply(operand);
                        }
                        else if (currentInstruction == Instruction.DIV)
                        {
                            Divide(operand);
                        }
                        else if (currentInstruction == Instruction.LD)
                        {
                            LoadFromMemory(operand);
                        }
                        else if (currentInstruction == Instruction.MM)
                        {
                            MoveToMemory(operand);
                        }
                        else if (currentInstruction == Instruction.SC)
                        {
                            SubroutineCall(operand);
                        }
                        else // Error - unidentified operation
                        {
                            OSCall(1);
                        }
                    }
                    maxPC = (maxPC > ProgramCounter) ? maxPC : ProgramCounter;
                    DebugMessage($"Accumulator value: {Accumulator:X2}");
                }
            }
            CheckMemoryAt(address, (ushort)(maxPC - address));
        }

        private void IOCall(char[] buffer, byte operand, StreamReader sr, StreamWriter sw)
        {
            // Read
            if ((operand & 0x0f) == 0)
            {
                if (sr.ReadBlock(buffer, 0, buffer.Length) < buffer.Length)
                {
                    DebugMessage("EOF!", ConsoleColor.DarkRed);
                    OSCall(1);
                }
                else
                {
                    Accumulator = (sbyte)Utilities.BufferToByte(buffer);
                    DebugMessage($"IO Read: {Accumulator:X2}");
                    ProgramCounter++;
                }
            }
            // Write
            else
            {
                sw.Write($"{Accumulator:X2} ");
                ProgramCounter++;
            }
        }

        // Pointer
        private ushort Address(ushort x)
        {
            if (!IsPointer)
            {
                return x;
            }
            else
            {
                IsPointer = false;
                return (ushort)(((Memory[x] << 8) | Memory[++x]) & 0x0fff);
            }
        }

        // Operações de desvio
        public void Jump(ushort operand)
        {
            ProgramCounter = Address(operand);
        }

        public void JumpZero(ushort operand)
        {
            if (Accumulator == 0)
            {
                ProgramCounter = Address(operand);
            }
            else
            {
                ProgramCounter++;
            }
        }

        public void JumpNegative(ushort operand)
        {
            if (Accumulator < 0)
            {
                ProgramCounter = Address(operand);
            }
            else
            {
                ProgramCounter++;
            }
        }

        // Arithmetic Operations
        public void Add(ushort operand)
        {
            Accumulator += (sbyte)Memory[Address(operand)];
            ProgramCounter++;
        }
        public void Subtract(ushort operand)
        {
            Accumulator -= (sbyte)Memory[Address(operand)];
            ProgramCounter++;
        }

        public void Multiply(ushort operand)
        {
            Accumulator *= (sbyte)Memory[Address(operand)];
            ProgramCounter++;
        }

        public void Divide(ushort operand)
        {
            Accumulator /= (sbyte)Memory[Address(operand)];
            ProgramCounter++;
        }

        // Memory Operations
        public void LoadFromMemory(ushort operand)
        {

            Accumulator = (sbyte)Memory[Address(operand)];
            ProgramCounter++;
        }

        public void MoveToMemory(ushort operand)
        {
            Memory[Address(operand)] = (byte)Accumulator;
            ProgramCounter++;
        }

        // Stack Operations
        public void SubroutineCall(ushort address)
        {
            IsPointer = false;
            CallStack.Push(ProgramCounter);
            ProgramCounter = address;
        }

        public void OSCall(byte operand)
        {
            IsPointer = false;
            if (operand == 0)
            {
                if (CallStack.Count > 0)
                {
                    ProgramCounter = (ushort)(CallStack.Pop() + 2);
                }
                else
                {
                    Control(0);
                }
            }
            else
            {
                // Error
                CallStack.Clear();
                ProgramCounter = 0;
                DebugMessage("Exit Code 1", ConsoleColor.DarkRed);
                Error = true;
                IsRunning = false;
            }
        }

        // Control Operation
        public void Control(byte operand)
        {
            IsPointer = false;
            // halt
            if (operand == 0)
            {
                DebugMessage("Exit Code 0", ConsoleColor.DarkGreen);
                IsRunning = false;
                Error = false;
            }
            // pointer operation
            else
            {
                IsPointer = true;
                ProgramCounter++;
            }
        }

        // Debug Functions
        private void DebugMessage(string message, ConsoleColor color = ConsoleColor.DarkYellow)
        {
            if (IsDebug)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        public void CheckMemoryAt(ushort address, ushort size)
        {
            if (IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                ushort last = (ushort)(address + size);
                last = (ushort) ((last > 0xfff) ? 0xfff : last);
                Utilities.ShowMemory(Memory, address, last);
                Console.WriteLine();
                Console.ResetColor();
            }
        }
    }
}