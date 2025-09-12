using System;
using Gearbox.Core.Interfaces;

namespace Gearbox.Core.Exceptions
{
    public class BrowserException : Exception
    {
        public BrowserException(string message, IBrowser browser = null) : base(message)
        {
            Browser = browser;
        }

        public IBrowser Browser { get; private set; }
    }
}
