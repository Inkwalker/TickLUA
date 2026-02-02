using System;

namespace TickLUA.VM
{
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message) { }
    }
}
