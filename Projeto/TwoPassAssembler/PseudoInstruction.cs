using System.Collections.Generic;

namespace Projeto.TwoPassAssembler
{
    public sealed class PseudoInstruction
    {
        public static readonly PseudoInstruction Origin = new PseudoInstruction("@");
        public static readonly PseudoInstruction End = new PseudoInstruction("#");
        public static readonly PseudoInstruction Constant = new PseudoInstruction("K");

        public string Mnemonic { get; }

        private PseudoInstruction(string mnemonic) => Mnemonic = mnemonic;

        public static IEnumerable<PseudoInstruction> GetList()
        {
            yield return Origin;
            yield return End;
            yield return Constant;
        }
    }
}
