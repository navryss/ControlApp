using System;

namespace Engine
{
    public class LogEntryEventArgs : EventArgs
    {
        public LogEntryEventArgs(String iLogEntry)
        { LogEntry = iLogEntry; }
        public String LogEntry { get; set; }
    }
}
