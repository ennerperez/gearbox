using System;
using Gearbox.Core.Models;

namespace Gearbox.Core.Exceptions
{
    public class BrowserException : Exception
    {
        public BrowserException(string message, Browser? browser = null) : base(message)
        {
            Browser = browser;
        }

        public Browser? Browser { get; private set; }
    }
}
