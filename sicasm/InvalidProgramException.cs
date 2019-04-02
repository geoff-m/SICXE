using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXEAssembler
{
    internal class InvalidProgramException : Exception
    {
        public InvalidProgramException(string message, Line line) : base(message)
        {
            Line = line;
        }

        public InvalidProgramException(string message, Line line, Exception innerException) : base(message, innerException)
        {
            Line = line;
        }

        public readonly Line Line;

        public override string ToString()
        {
            if (Line == null)
            {
                if (InnerException != null)
                    return $"{Message} ({InnerException})";
                return Message;
            }
            if (InnerException != null)
                return $"{Message} (Line {Line.LineNumber}) ({InnerException})";
            return $"{Message} (Line {Line.LineNumber})";
        }
    }
}
