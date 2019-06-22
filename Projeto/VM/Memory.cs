using System.Collections.Generic;

namespace Projeto.VM
{
    public class Memory
    {
        private List<byte> Data { get; } = new List<byte>(0x1000);
        public Memory()
        {
            // initialize memory with "random" values to avoid exceptions.
            for (int i = 0; i < Data.Capacity; i++)
            {
                Data.Add((byte)(0xff));
            }
        }

        public byte this[ushort address]
        {
            // Load From Memory
            get => Data[address];
            // Move to Memory
            set => Data[address] = value;
        }
    }
}
