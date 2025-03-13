using System;
using System.Collections.Generic;
using CheckRelease.Interfaces;

namespace CheckRelease.Testing
{
    /// <summary>
    /// Mock implementation of IConsoleOutput for testing.
    /// </summary>
    public class MockConsoleOutput : IConsoleOutput
    {
        /// <summary>
        /// Gets the collection of messages written to standard output.
        /// </summary>
        public List<string> StandardOutput { get; } = new List<string>();
        
        /// <summary>
        /// Gets the collection of messages written to standard error.
        /// </summary>
        public List<string> ErrorOutput { get; } = new List<string>();
        
        /// <summary>
        /// Gets the collection of debug messages.
        /// </summary>
        public List<string> DebugOutput { get; } = new List<string>();
        
        /// <summary>
        /// Gets the queue of input lines to be returned by ReadLine.
        /// </summary>
        public Queue<string?> InputQueue { get; } = new Queue<string?>();
        
        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled.
        /// </summary>
        public bool DebugMode { get; set; }
        
        /// <inheritdoc/>
        public void WriteLine(string message)
        {
            StandardOutput.Add(message);
        }
        
        /// <inheritdoc/>
        public void WriteDebug(string message)
        {
            DebugOutput.Add(message);
            if (DebugMode)
            {
                StandardOutput.Add(message);
            }
        }
        
        /// <inheritdoc/>
        public void WriteError(string message)
        {
            ErrorOutput.Add(message);
        }
        
        /// <inheritdoc/>
        public void Write(string message)
        {
            StandardOutput.Add(message);
        }
        
        /// <inheritdoc/>
        public string? ReadLine()
        {
            return InputQueue.Count > 0 ? InputQueue.Dequeue() : null;
        }
        
        /// <summary>
        /// Clears all output collections.
        /// </summary>
        public void Clear()
        {
            StandardOutput.Clear();
            ErrorOutput.Clear();
            DebugOutput.Clear();
        }
        
        /// <summary>
        /// Enqueues a line of input to be returned by ReadLine.
        /// </summary>
        /// <param name="input">The input to enqueue.</param>
        public void EnqueueInput(string? input)
        {
            InputQueue.Enqueue(input);
        }
    }
}
