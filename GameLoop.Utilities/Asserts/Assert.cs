using System.Diagnostics;

namespace GameLoop.Utilities.Asserts
{
    public static class Assert
    {
        [Conditional("DEBUG")]
        public static void Fail()
        {
            throw new AssertFailedException();
        }

        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            throw new AssertFailedException(message);
        }

        [Conditional("DEBUG")]
        public static void Fail(string message, params object[] parameters)
        {
            throw new AssertFailedException(string.Format(message, parameters));
        }
        
        [Conditional("DEBUG")]
        public static void Check(bool condition)
        {
            if (condition == false) Fail();
        }
        
        [Conditional("DEBUG")]
        public static void Check(bool condition, string message)
        {
            if (condition == false) Fail(message);
        }
        
        [Conditional("DEBUG")]
        public static void Check(bool condition, string message, params object[] parameters)
        {
            if (condition == false) Fail(message, parameters);
        }
    }
}