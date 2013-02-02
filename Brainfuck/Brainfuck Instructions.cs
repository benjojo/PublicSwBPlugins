using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brainfuck
{
    public partial class Brainfuck
    {
        [BrainfuckInstruction('>', "Increments memory pointer.")]
        static void MoveRight(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            memoryPointer++;
            if (memoryPointer >= MemoryLimit) { throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Out of memory."); }

            while (memory.Count <= memoryPointer)
            {
                memory.Add(0);
            }
        }

        [BrainfuckInstruction('<', "Decrements memory pointer.")]
        static void MoveLeft(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            memoryPointer--;
            if (memoryPointer < 0) { throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Tried to move to negative memory."); }
        }

        [BrainfuckInstruction('+', "Increments value at memory pointer.")]
        static void Increment(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        { unchecked { memory[memoryPointer]++; } }

        [BrainfuckInstruction('-', "Decrements value at memory pointer.")]
        static void Decrement(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        { unchecked { memory[memoryPointer]--; } }

        [BrainfuckInstruction('.', "Outputs character (UTF-32).")]
        static void Output(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            try { output.Append(char.ConvertFromUtf32(memory[memoryPointer])); }
            catch
            {
                throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Could not output value " + memory[memoryPointer]);
            }
        }

        [BrainfuckInstruction(',', "Inputs character (UTF-32). Reading past the input returns 0.")]
        static void Input(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            if (inputPointer >= input.Length)
            {
                memory[memoryPointer] = 0;
                return;
            }

            try
            {
                memory[memoryPointer] = char.ConvertToUtf32(input, inputPointer);
                inputPointer += char.ConvertFromUtf32(memory[memoryPointer]).Length;
            }
            catch (Exception ex)
            {
                throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Could not input character at " + inputPointer);
            }
        }

        [BrainfuckInstruction('[', "Jumps past matching ] if value at memory pointer is 0.")]
        static void LoopStart(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            if (memory[memoryPointer] == 0)
            {
                int loopStart = instructionPointer;


                instructionPointer++;

                for (int depth = 1; depth != 0; instructionPointer++)
                {
                    if (instructionPointer >= program.Length) { throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Open loop starting at " + loopStart); }

                    switch (program[instructionPointer])
                    {
                        case '[': depth++; break;
                        case ']': depth--; break;
                        default: break;
                    }
                }

                instructionPointer--;
            }
        }

        [BrainfuckInstruction(']', "Jumps back to matching [ if value at memory pointer is not 0.")]
        static void LoopEnd(string program, List<int> memory, ref int instructionPointer, ref int memoryPointer, string input, ref int inputPointer, StringBuilder output)
        {
            if (memory[memoryPointer] != 0)
            {
                int loopEnd = instructionPointer;

                instructionPointer--;

                for (int depth = 1; depth != 0; instructionPointer--)
                {
                    if (instructionPointer < 0) { throw new BrainfuckException(program, memory, instructionPointer, memoryPointer, input, inputPointer, output, "Open loop ending at " + loopEnd); }

                    switch (program[instructionPointer])
                    {
                        case ']': depth++; break;
                        case '[': depth--; break;
                        default: break;
                    }
                }

                instructionPointer++;
            }
        }
    }
}
