using System;

namespace SymphonyFrameWork.Exceptions
{
    public class SymphonyNotInitializedException : Exception
    {
        public SymphonyNotInitializedException() : base("SymphonyFrameworkが未初期化です。")
        {
        }
        public SymphonyNotInitializedException(string message) : base(message)
        {
        }
        public SymphonyNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
