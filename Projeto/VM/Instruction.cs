using System.Collections.Generic;

namespace Projeto.VM
{
    public sealed class Instruction
    {
        public static readonly Instruction JP = new Instruction(0x0000, 2);
        public static readonly Instruction JZ = new Instruction(0x1000, 2);
        public static readonly Instruction JN = new Instruction(0x2000, 2);
        public static readonly Instruction CN = new Instruction(0x3000, 1);
        public static readonly Instruction ADD = new Instruction(0x4000, 2);
        public static readonly Instruction SUB = new Instruction(0x5000, 2);
        public static readonly Instruction MULT = new Instruction(0x6000, 2);
        public static readonly Instruction DIV = new Instruction(0x7000, 2);
        public static readonly Instruction LD = new Instruction(0x8000, 2);
        public static readonly Instruction MM = new Instruction(0x9000, 2);
        public static readonly Instruction SC = new Instruction(0xA000, 2);
        public static readonly Instruction OS = new Instruction(0xB000, 1);
        public static readonly Instruction IO = new Instruction(0xC000, 1);
    
        public ushort Opcode { get; }
        public byte Size { get; }
        private Instruction(ushort opcode, byte size)
        {
            Opcode = opcode;
            Size = size;
        }

        public static IEnumerable<Instruction> GetList()
        {
            yield return JP;
            yield return JZ;
            yield return JN;
            yield return CN;
            yield return ADD;
            yield return SUB;
            yield return MULT;
            yield return DIV;
            yield return LD;
            yield return MM;
            yield return SC;
            yield return OS;
            yield return IO;
        }
    }
}
