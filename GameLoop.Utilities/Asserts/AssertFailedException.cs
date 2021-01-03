using System;

namespace GameLoop.Utilities.Asserts
{
    public class AssertFailedException : Exception
    {
        public AssertFailedException() {}
        public AssertFailedException(string message) : base(message) {}
    }
}