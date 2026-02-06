using Eco.Core.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnownToAll
{
    static class Logger
    {
        const string NAME = "KnownToAll";

        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            NLogManager.GetEcoLogWriter().Write($"[{NAME}] {message}\n");
        }

        public static void Info(string message)
        {
            NLogManager.GetEcoLogWriter().Write($"[{NAME}] {message}\n");
        }
    }
}
