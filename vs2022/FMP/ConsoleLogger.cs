using Microsoft.Extensions.Logging;
using System;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class ConsoleLogger : Logger
    {
        
        protected override void trace(string _category, string _message)
        {
            Console.WriteLine($"TRACE [{_category}] - {_message}");
        }

        protected override void debug(string _category, string _message)
        {
            Console.WriteLine($"DEBUG [{_category}] - {_message}");
        }

        protected override void info(string _category, string _message)
        {
            Console.WriteLine($"INFO [{_category}] - {_message}");
        }

        protected override void warning(string _category, string _message)
        {
            Console.WriteLine($"WARN [{_category}] - {_message}");
        }

        protected override void error(string _category, string _message)
        {
            Console.WriteLine($"ERROR [{_category}] - {_message}");
        }

        protected override void exception(Exception _exception)
        {
            Console.WriteLine($"EXCEPTION  - {_exception.StackTrace}");
        }
    }
}
