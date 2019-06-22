using System;
using System.IO;
using Projeto.VM;
namespace Projeto
{
    public static class Utilities
    {
        public static ushort StringToAddress(string hex)
        {
            return ushort.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        public static byte StringToByte(string hex)
        {
            return byte.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        public static byte CharToByte(char c)
        {
            return (byte)"0123456789ABCDEF".IndexOf(char.ToUpper(c));
        }

        public static byte BufferToByte(char[] buffer)
        {
            byte high = CharToByte(buffer[0]);
            byte low = CharToByte(buffer[1]);
            return (byte)((high << 4) | low);
        }

        public static string AddressToString(ushort address)
        {
            return address.ToString("X4");
        }

        public static string ByteToString(byte value)
        {
            return value.ToString("X2");
        }

        public static void ShowMemory(Memory mem, ushort first, ushort last)
        {
            Console.WriteLine($"Memory at {first:X4}");
            Console.WriteLine("    0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
            for (ushort i = (ushort)(first & 0xff0); i <= (ushort)(last & 0xff0); i += 0x010)
            {
                Console.Write($"{i >> 4:X2} ");
                for (ushort j = 0; j < 0x10; j++)
                {
                    Console.Write($"{mem[(ushort)(i | j)]:X2} ");
                }
                Console.Write("\n");
            }
        }

        public static void AddressFromFile(FileInfo f, out ushort address, out ushort length)
        {
            using (StreamReader sr = new StreamReader(f.FullName))
            {
                char[] buffer = new char[3];
                sr.ReadBlock(buffer, 0, 3);
                address = (ushort)(BufferToByte(buffer) << 8);
                sr.ReadBlock(buffer, 0, 3);
                address = (ushort)(address | BufferToByte(buffer));
                sr.ReadBlock(buffer, 0, 3);
                length = BufferToByte(buffer);
            }
        }

        public static void ShowDumpedMemory(FileInfo file)
        {
            using (StreamReader sr = new StreamReader(file.FullName))
            {
                char[] buffer = new char[3];
                sr.ReadBlock(buffer, 0, 3);
                ushort address = (ushort)(BufferToByte(buffer) << 8);
                sr.ReadBlock(buffer, 0, 3);
                address = (ushort)(address | BufferToByte(buffer));
                sr.ReadBlock(buffer, 0, 3);
                byte length = BufferToByte(buffer);
                Console.WriteLine($"First address: {address:X4}");
                Console.WriteLine($"Length: {length:X2}");
                Console.WriteLine("\n    0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
                for (int i = address & 0xff0; !sr.EndOfStream && length > 1; i+=0x10)
                {
                    Console.Write($"{i >> 4:X2} ");
                    for (int j = 0; j < 0x10; j++)
                    {
                        if ((i | j) < address)
                        {
                            Console.Write("FF ");
                        }
                        else if (sr.EndOfStream)
                        {
                            Console.Write("FF ");
                        }
                        else if (length < 1)
                        {
                            Console.Write("FF ");
                        }
                        else
                        {
                            length--;
                            sr.ReadBlock(buffer, 0, 3);
                            Console.Write($"{BufferToByte(buffer):X2} ");
                        }
                    }
                    Console.Write("\n");
                }
            }
        }
    }
}
