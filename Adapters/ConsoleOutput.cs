using System;
using CheckRelease.Interfaces;

namespace CheckRelease.Adapters
{
    /// <summary>
    /// Standard implementation of IConsoleOutput that writes to the console.
    /// </summary>
    public class ConsoleOutput : IConsoleOutput
    {
        private readonly bool _debugMode;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleOutput"/> class.
        /// </summary>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        public ConsoleOutput(bool debugMode = false)
        {
            _debugMode = debugMode;
        }
        
        /// <inheritdoc/>
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
        
        /// <inheritdoc/>
        public void WriteDebug(string message)
        {
            if (_debugMode)
            {
                Console.WriteLine(message);
            }
        }
        
        /// <inheritdoc/>
        public void WriteError(string message)
        {
            Console.Error.WriteLine(message);
        }
        
        /// <inheritdoc/>
        public void Write(string message)
        {
            Console.Write(message);
        }
        
        /// <inheritdoc/>
        public string? ReadLine()
        {
            return Console.ReadLine();
        }
    }
}
