using discord.core;
using discord.plugins;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Brainfuck
{
    public partial class Brainfuck : IPlugin
    {
        public const int InstructionLimit = 1000000;
        public const int MemoryLimit = 1000;

        public string Name { get { return "brainfuck"; } }
        public string Auth { get { return "Tamschi"; } }
        public string Desc { get { return "A brainfuck interpreter.\nInstructionLimit: " + InstructionLimit + "\nMemoryLimit: " + MemoryLimit; } }

        public void Load() { Events.OnChatMsgCallback += ChatMsgHandler; }
        public void Unload() { Events.OnChatMsgCallback -= ChatMsgHandler; }

        void ChatMsgHandler(SteamFriends.ChatMsgCallback msg)
        {
            var lower = msg.Message.ToLowerInvariant();

            if (lower.StartsWith("sb") &&
                (lower.Contains("brainfuck") || lower.Split(' ').Contains("bf")) &&
                    (lower.Contains("help") || lower.Contains("instructions") || lower.Contains("manual") || lower.Split(' ').Contains("man")))
            {
                Discord.SendChatMessage(msg.ChatRoomID, "InstructionLimit: " + InstructionLimit + "\nMemoryLimit: " + MemoryLimit + "\n" + InstructionHelp);
            }

            else { TryParseRunProgram(msg); }
        }

        private static void TryParseRunProgram(SteamFriends.ChatMsgCallback msg)
        {
            var spacePos = msg.Message.IndexOf(' ');

            string program;
            string input;
            if (spacePos == -1)
            {
                program = msg.Message;
                input = "";
            }
            else
            {
                program = msg.Message.Substring(0, spacePos);
                input = msg.Message.Substring(spacePos + 1);
            }

            foreach (var c in program)
            {
                if (_instructions.ContainsKey(c) == false) return;
            }

            var result = RunProgram(program, input);
            Discord.SendChatMessage(msg.ChatRoomID, result);
        }

        public static string RunProgram(string program, string input)
        {
            var memory = new List<int>() { 0 };

            int instructionPointer = 0;
            int memoryPointer = 0;
            int inputPointer = 0;

            var output = new StringBuilder();

            var instructions = 0;

            try
            {
                while (instructionPointer < program.Length)
                {
                    _instructions[program[instructionPointer]](program, memory, ref instructionPointer, ref memoryPointer, input, ref inputPointer, output);

                    instructions++;
                    if (instructions >= InstructionLimit) {throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Instruction limit reached."); }

                    instructionPointer++;
                }
            }
            catch (BrainfuckException ex)
            {
                var outPartString = output.ToString();

                return "Execution failed.\n" +
                    "Error message: " + ex.Message + "\n" +
                    "Instruction pointer: " + ex.InstructionPointer + "\n" +
                    "Memory pointer: " + ex.MemoryPointer + "\n" +
                    "Input pointer: " + ex.InputPointer + "\n" +
                    "Instructions executed: " + instructions + "\n" +
                    "Output:" + (outPartString.Contains('\n') ? "\n" : " ") + outPartString;
            }
            catch (Exception ex)
            {
                var outPartString = output.ToString();

                return "Execution failed.\n" +
                    "Error message: " + ex.Message + "\n" +
                    "Instruction pointer: " + instructionPointer + "\n" +
                    "Memory pointer: " + memoryPointer + "\n" +
                    "Input pointer: " + inputPointer + "\n" +
                    "Instructions executed: " + instructions + "\n" +
                    "Output:" + (outPartString.Contains('\n') ? "\n" : " ") + outPartString;
            }

            var outString = output.ToString();

            return "Instructions executed: " + instructions + "\n" +
                "Output:" + (outString.Contains('\n') ? "\n" : " ") + outString;
        }

        public class BrainfuckException : Exception
        {
            public string Program { get; set; }
            public List<int> Memory { get; set; }
            public int InstructionPointer { get; set; }
            public int MemoryPointer { get; set; }
            public string Input { get; set; }
            public int InputPointer { get; set; }
            public string Output { get; set; }

            public BrainfuckException(
                string program,
                List<int> memory,
                int instructionPointer,
                int memoryPointer,
                string input,
                int inputPointer,
                StringBuilder output,
                string message)
                : base(message)
            {
                Program = program;
                Memory = memory;
                InstructionPointer = instructionPointer;
                MemoryPointer = memoryPointer;
                Input = input;
                InputPointer = inputPointer;
                Output = output.ToString();
            }
        }

        delegate void BrainfuckInstruction(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output);

        static Dictionary<char, BrainfuckInstruction> _instructions;

        public static string InstructionHelp { get; private set; }

        static Brainfuck()
        {
            InstructionHelp = "Instructions:";

            _instructions = new Dictionary<char, BrainfuckInstruction>();

            foreach (var method in typeof(Brainfuck).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                BrainfuckInstructionAttribute attrib = method.GetCustomAttribute<BrainfuckInstructionAttribute>();
                if (attrib == null) { continue; }

                BrainfuckInstruction @delegate;
                try
                {
                    @delegate = (BrainfuckInstruction)method.CreateDelegate(typeof(BrainfuckInstruction));
                }
                catch (Exception ex) { throw new InvalidProgramException("Could not create instruction delegate for method " + method.Name, ex); }

                _instructions.Add(attrib.InstructionCode, @delegate);
                InstructionHelp += "\n    " + attrib.InstructionCode + ": " + attrib.Help;
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        sealed class BrainfuckInstructionAttribute : Attribute
        {
            readonly char _instructionCode;
            readonly string _help;

            public BrainfuckInstructionAttribute(char instructionCode, string help)
            {
                this._instructionCode = instructionCode;
                this._help = help;
            }

            public char InstructionCode { get { return _instructionCode; } }

            public string Help { get { return _help; } }
        }


    }
}
