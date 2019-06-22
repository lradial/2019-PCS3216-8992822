using System;
using System.IO;
using Projeto.TwoPassAssembler;
using Projeto.VM;
using System.Reflection;
using System.Collections.Generic;

namespace Projeto
{
    class Program
    {
        private const string ExeLoaderResource = "Projeto.src.exe.exeLoader.txt";
        private const string LoaderResource = "Projeto.src.vm.Loader.txt";
        private const string DumperResource = "Projeto.src.vm.Dumper.txt";
        private const string N2Resource = "Projeto.src.usr.n2.txt";
        private const string AssembledString = "exe";
        private const ushort LoaderAddress = 0x000;
        private const ushort LoaderLength = 0x4B;
        private const ushort DumperAddress = 0xfc0;
        private const ushort DumperLength = 0x40;
        private const int MaxAddress = 0x0fff;
        private const string DumperObjectFile = "exeDumper.txt";
        private const string DumperSourceFile = "Dumper.txt";

        private static DirectoryInfo SourceFiles { get; set; }
        private static DirectoryInfo ObjectFiles { get; set; }
        private static FileInfo InputFile { get; set; }
        private static FileInfo OutputFile { get; set; }
        private enum MenuOptions
        {
            ListSrc = 0,
            Assembly = 1,
            ListObj = 2,
            Load = 3,
            Run = 4,
            Memory = 5,
            Accumulator = 6,
            Exit = 7
        }
        private static VirtualMachine VM { get; set; }
        private static Assembler Assembler { get; set; }

        // For convenience, mostly.
        private static Dictionary<ushort, Tuple<string, ushort >> LoadedPrograms { get; set; }

        /// <summary>
        /// Creates necessary files and directories for the VM.
        /// </summary>
        static void Initialize(bool overwrite, bool debug)
        {
            string cd = Directory.GetCurrentDirectory();
            // IO Directory
            DirectoryInfo ioFiles = new DirectoryInfo(Path.Combine(cd, "IO Files"));
            ioFiles.Create();
            // do not overwrite existing files
            InputFile = new FileInfo(Path.Combine(ioFiles.FullName, "input.txt"));
            if (overwrite || !InputFile.Exists)
            {
                InputFile.Create().Dispose();
            }
            OutputFile = new FileInfo(Path.Combine(ioFiles.FullName, "output.txt"));
            if (overwrite || !OutputFile.Exists)
            {
                OutputFile.Create().Dispose();
            }

            // Assembler Directory
            DirectoryInfo AssemblerFiles = new DirectoryInfo(Path.Combine(cd, "Assembler"));
            SourceFiles = Directory.CreateDirectory(Path.Combine(AssemblerFiles.FullName, "src"));
            ObjectFiles = Directory.CreateDirectory(Path.Combine(AssemblerFiles.FullName, "obj"));

            // Create Embedded files if needed
            // Source files - VM programs
            // Loader
            SaveResourceAsFile(LoaderResource, Path.Combine(SourceFiles.FullName, "Loader.txt"), overwrite);
            // Dumper
            SaveResourceAsFile(DumperResource, Path.Combine(SourceFiles.FullName, DumperSourceFile), overwrite);
            // Source files - user programs
            SaveResourceAsFile(N2Resource, Path.Combine(SourceFiles.FullName, "n2.txt"), overwrite);
            // Object code files
            SaveResourceAsFile(ExeLoaderResource, Path.Combine(ObjectFiles.FullName, "exeLoader.txt"), overwrite);

            // Create VM and Assembler instances
            Assembler = new Assembler();
            VM = new VirtualMachine(InputFile, OutputFile, debug);
            // Manually load Loader into VM's memory
            ProgramLoader();

            // Create LoadedProgram list
            LoadedPrograms = new Dictionary<ushort, Tuple<string, ushort>>()
            {
                { LoaderAddress, new Tuple<string, ushort>("Loader", LoaderAddress+LoaderLength-1) }
            };
        }

        /// <summary>
        /// Lists Source files showing 
        /// </summary>
        static FileInfo[] ListSourceFiles()
        {
            Console.WriteLine($"Listing source files in");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{SourceFiles.FullName}\n");
            Console.ResetColor();
            string loadable = "*";
            int i = 0;
            FileInfo[] files = SourceFiles.GetFiles("*.txt");
            foreach (var file in files)
            {
                Console.Write($"{i++}");
                if (File.Exists(Path.Combine(ObjectFiles.FullName, AssembledString + file.Name)))
                {
                    Console.Write(loadable);
                }
                Console.Write($" {Path.GetFileNameWithoutExtension(file.FullName)}\n");
            }
            Console.WriteLine($"\nFiles with {loadable} have already been assembled.\n");
            return files;
        }

        /// <summary>
        /// Cleans Console window, shows a list of available source files and chooses a file for assembly
        /// </summary>
        static void AssembleFile()
        {
            int num;
            FileInfo[] files = ListSourceFiles();
            Console.WriteLine("Which file do you want to assemble?");
            while (!int.TryParse(Console.ReadLine(), out num) || num < 0 || num >= files.Length)
            {
                Console.WriteLine("Invalid answer!");
                Console.WriteLine("Which file do you want to assemble?");
            }
            FileInfo src = files[num];
            FileInfo output = new FileInfo(Path.Combine(ObjectFiles.FullName, AssembledString + src.Name));
            Assembler.Assembly(src, output);
        }

        /// <summary>
        /// Creates a file from a resource.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="filePath"></param>
        private static void SaveResourceAsFile(string resourceName, string filePath, bool overwrite)
        {
            if (!overwrite && File.Exists(filePath))
            {
                // Do not overwrite existing file
                return;
            }
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (StreamReader sr = new StreamReader(stream))
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                while (!sr.EndOfStream)
                {
                    sw.WriteLine(sr.ReadLine());
                }
            }
        }

        /// <summary>
        /// Manually loads the Loader executable program into the virtual machine's memory. The Loader code has been embedded as resource.
        /// </summary>
        static void ProgramLoader()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ExeLoaderResource))
            using (StreamReader sr = new StreamReader(stream))
            {
                char[] buffer = new char[3];
                // Address
                sr.ReadBlock(buffer, 0, 3);
                ushort address = (ushort)(Utilities.BufferToByte(buffer) << 8);
                sr.ReadBlock(buffer, 0, 3);
                address = (ushort)(address | Utilities.BufferToByte(buffer));
                // size
                sr.ReadBlock(buffer, 0, 3);
                byte length = Utilities.BufferToByte(buffer);
                for (ushort i = 0; i < length; i++)
                {
                    sr.ReadBlock(buffer, 0, 3);
                    VM.Memory[(ushort)(address + i)] = Utilities.BufferToByte(buffer);
                }
            }
        }

        /// <summary>
        /// Lists object code files
        /// </summary>
        /// <returns>FileInfo[] with object code files</returns>
        static FileInfo[] ListLoadablePrograms()
        {
            int i = 0;
            Console.WriteLine($"Listing object code files in");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{ObjectFiles.FullName}\n");
            Console.ResetColor();
            FileInfo[] files = ObjectFiles.GetFiles("*.txt");
            foreach (var file in files)
            {
                Console.WriteLine($"{i++} {Path.GetFileNameWithoutExtension(file.FullName)}");
            }
            return files;
        }

        /// <summary>
        /// Lists object code files, copies the selected code to VM's input file and runs the loader program.
        /// </summary>
        static void LoadProgram()
        {
            int num;
            FileInfo[] files = ListLoadablePrograms();
            Console.WriteLine("\nWhich file do you want to load?");
            while (!int.TryParse(Console.ReadLine(), out num) || num < 0 || num >= files.Length)
            {
                Console.WriteLine("Invalid answer!");
                Console.WriteLine("Which file do you want to load?");
            }
            // Copy object code to VM's input file
            File.Copy(files[num].FullName, InputFile.FullName, true);
            // Loader
            VM.Run(LoaderAddress);
            // For convenience mostly.
            // Add to LoadedPrograms list
            Utilities.AddressFromFile(files[num], out ushort address, out ushort length);
            try
            {
                string name = files[num].Name;
                name = name.Substring(3, name.Length - 7);
                LoadedPrograms.Add(address, new Tuple<string, ushort>(name, (ushort)(address + length - 1)));
            }
            catch (Exception)
            {
                Console.WriteLine("This program was already loaded before.");
            }
            if (VM.Error)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Note: The programmed loader does not correctly performs the checksum, so ignore the following warning.");
                Console.ResetColor();
                Console.WriteLine($"The checksum could not be verified, the program {LoadedPrograms[address].Item1} loaded at {address:X4} could be corrupt.");
            }
        }

        /// <summary>
        /// Lists currently loaded programs and asks for a starting address to start execution.
        /// </summary>
        static void RunProgram()
        {
            ListLoadedPrograms();
            Console.WriteLine("\nWhat's the first address of the program?");
            ushort num = (ushort)(Utilities.StringToAddress(Console.ReadLine()) & MaxAddress);
            VM.Run(num);
        }

        /// <summary>
        /// Lists currently loaded programs
        /// </summary>
        private static void ListLoadedPrograms()
        {
            Console.WriteLine("Loaded programs:\n");
            Console.WriteLine("Initial Address - Last Address - Program file");
            foreach (var item in LoadedPrograms)
            {
                Console.WriteLine($"{item.Key:X4} - {item.Value.Item2:X4} - {item.Value.Item1}");
            }
        }

        static void MemoryMap()
        {
            ListLoadedPrograms();
            Console.WriteLine("\nWhat's the first address?");
            ushort first = (ushort)(Utilities.StringToAddress(Console.ReadLine()) & MaxAddress);
            Console.WriteLine("What's the final address?");
            ushort last = (ushort)(Utilities.StringToAddress(Console.ReadLine()) & MaxAddress);
            Console.WriteLine();
            ushort len = (ushort)((last + 1 - first) & 0x00ff);
            if (len == 0)
            {
                return;
            }
            // Creates a copy of the Dumper source file so it dumps the right program
            FileInfo src = new FileInfo(Path.Combine(SourceFiles.FullName, DumperSourceFile));
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DumperResource))
            using (StreamReader sr = new StreamReader(stream))
            using (StreamWriter sw = new StreamWriter(src.FullName))
            {
                int lineNum = 0;
                while (!sr.EndOfStream)
                {
                    if (lineNum == 39)
                    {
                        sr.ReadLine();
                        lineNum++;
                        sr.ReadLine();
                        lineNum++;
                        sr.ReadLine();
                        lineNum++;
                        sw.WriteLine($"SIZE K {len:X2}");
                        sw.WriteLine($"PTR1 K {(first & 0xf00)>>8:X2}");
                        sw.WriteLine($"PTR2 K {first & 0x0ff:X2}");

                    }
                    else
                    {
                        sw.WriteLine(sr.ReadLine());
                        lineNum++;
                    }
                }
            }


            // Dumper assembly
            FileInfo objDumper =  new FileInfo(Path.Combine(ObjectFiles.FullName, DumperObjectFile));
            Assembler.Assembly(src, objDumper);

            // Load Dumper into VM
            File.Copy(objDumper.FullName, InputFile.FullName, true);
            VM.Run(LoaderAddress);

            // Run Dumper program
            VM.Run(DumperAddress);
            Console.WriteLine("\nMemory dumped to output file.\n");
            // Compare Dumper output with current memory readings
            Console.ForegroundColor = ConsoleColor.Cyan;
            Utilities.ShowMemory(VM.Memory, first, last);
            Console.ResetColor();

            Console.WriteLine("\nDumper output: \n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Utilities.ShowDumpedMemory(OutputFile);
            Console.ResetColor();
        }

        private static void AccumulatorValue()
        {
            Console.WriteLine($"Accumulator value: {VM.Accumulator:X2}");
        }

        /// <summary>
        /// Console user interface
        /// </summary>
        static void Main(string[] args)
        {
            int num;
            bool exit = false;
            bool debug = true;
            bool overwrite = false;
            Console.Title = "Initializing VM";
            Console.WriteLine("Do you wish to use the debug mode? (y/n)");
            switch (Console.ReadLine())
            {
                case "y":
                    debug = true;
                    break;
                case "n":
                    debug = false;
                    break;
                default:
                    debug = true;
                    break;
            }
            Console.WriteLine("Do you wish to overwrite files with default ones? (y/n)");
            switch (Console.ReadLine())
            {
                case "y":
                    overwrite = true;
                    break;
                case "n":
                    overwrite = false;
                    break;
                default:
                    overwrite = true;
                    break;
            }
            Initialize(overwrite, debug);
            while (!exit)
            {
                Console.Title = "Virtual Machine Console - Main Menu";
                Console.Clear();
                Console.WriteLine("Main Menu\n");
                Console.WriteLine($"{(int)MenuOptions.ListSrc} List source files;");
                Console.WriteLine($"{(int)MenuOptions.Assembly} Assembly;");
                Console.WriteLine($"{(int)MenuOptions.ListObj} List object code files;");
                Console.WriteLine($"{(int)MenuOptions.Load} Load from object code file;");
                Console.WriteLine($"{(int)MenuOptions.Run} Run from address;");
                Console.WriteLine($"{(int)MenuOptions.Memory} Show memory map;");
                Console.WriteLine($"{(int)MenuOptions.Accumulator} Show accumulator value;");
                Console.WriteLine($"{(int)MenuOptions.Exit} Exit.\n");
                Console.WriteLine("Choose option:");
                while (!int.TryParse(Console.ReadLine(), out num) || num < (int)MenuOptions.ListSrc || num > (int)MenuOptions.Exit)
                {
                    Console.WriteLine("Invalid answer!");
                    Console.WriteLine("Choose option:");
                }
                switch ((MenuOptions)num)
                {
                    case MenuOptions.ListSrc:
                        Console.Title = "Virtual Machine Console - List source files";
                        Console.Clear();
                        ListSourceFiles();
                        break;
                    case MenuOptions.Assembly:
                        Console.Title = "Virtual Machine Console - Assembly";
                        Console.Clear();
                        AssembleFile();
                        break;
                    case MenuOptions.ListObj:
                        Console.Title = "Virtual Machine Console - List loadable programs";
                        Console.Clear();
                        ListLoadablePrograms();
                        break;
                    case MenuOptions.Load:
                        Console.Title = "Virtual Machine Console - Load program";
                        Console.Clear();
                        LoadProgram();
                        break;
                    case MenuOptions.Run:
                        Console.Title = "Virtual Machine Console - Run program";
                        Console.Clear();
                        RunProgram();
                        break;
                    case MenuOptions.Memory:
                        Console.Title = "Virtual Machine Console - Dump memory";
                        Console.Clear();
                        MemoryMap();
                        break;
                    case MenuOptions.Accumulator:
                        Console.Title = "Virtual Machine Console - Show accumulator value";
                        Console.Clear();
                        AccumulatorValue();
                        break;
                    case MenuOptions.Exit:
                        Console.Title = "Virtual Machine Console - Exit";
                        Console.Clear();
                        exit = true;
                        break;
                    default:
                        break;
                }
                WaitUser();
            }
        }

        /// <summary>
        /// Simply waits for user input.
        /// </summary>
        static void WaitUser()
        {
            ConsoleKeyInfo cki;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Press the Escape (Esc) key to continue:\n");
            Console.ResetColor();
            do
            {
                cki = Console.ReadKey();
                // do something with each key press until escape key is pressed
            } while (cki.Key != ConsoleKey.Escape);
        }
    }
}