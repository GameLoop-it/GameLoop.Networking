using System;

namespace GameLoop.Utilities.Exceptions
{
    public static class ExceptionThrower
    {
        public static void Throw<TException>() where TException : Exception, new()
        {
            Throw(new TException());
        }
        
        public static void Throw<TException>(TException exception) where TException : Exception, new()
        {
            throw exception;
        }
    }
}