using System.Collections.Generic;
using System.Text;

namespace CityScooter.Protocol
{
    /// <summary>
    /// Turns arbitrary chunks of serial text into complete lines. Serial data arrives in
    /// pieces ("cr" + "ash\n"), so nothing may be parsed until its newline has arrived.
    /// Handles "\r\n". Over-long lines are truncated rather than growing without bound.
    /// </summary>
    public sealed class LineFramer
    {
        readonly StringBuilder _buffer = new StringBuilder();
        readonly int _maxLineLength;

        public LineFramer(int maxLineLength = 256)
        {
            _maxLineLength = maxLineLength;
        }

        /// <summary>Appends a chunk; adds every completed line to <paramref name="output"/>. Returns how many.</summary>
        public int Push(string chunk, List<string> output)
        {
            if (string.IsNullOrEmpty(chunk)) return 0;
            int count = 0;
            foreach (char c in chunk)
            {
                if (c == '\n')
                {
                    int len = _buffer.Length;
                    if (len > 0 && _buffer[len - 1] == '\r') len--;
                    output.Add(_buffer.ToString(0, len));
                    _buffer.Clear();
                    count++;
                }
                else if (_buffer.Length < _maxLineLength)
                {
                    _buffer.Append(c);
                }
            }
            return count;
        }

        public bool HasPartialLine => _buffer.Length > 0;

        public void Reset() => _buffer.Clear();
    }
}
