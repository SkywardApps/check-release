using System;

namespace CheckRelease.Interfaces
{
    /// <summary>
    /// Interface for console output operations.
    /// </summary>
    public interface IConsoleOutput
    {
        /// <summary>
        /// Writes a line of text to the standard output stream.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void WriteLine(string message);
        
        /// <summary>
        /// Writes a line of text to the standard output stream if debug mode is enabled.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void WriteDebug(string message);
        
        /// <summary>
        /// Writes a line of text to the standard error stream.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void WriteError(string message);
        
        /// <summary>
        /// Writes text to the standard output stream without a line terminator.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void Write(string message);
        
        /// <summary>
        /// Reads a line of text from the standard input stream.
        /// </summary>
        /// <returns>The next line of text from the input stream, or null if no more lines are available.</returns>
        string? ReadLine();
    }
}
