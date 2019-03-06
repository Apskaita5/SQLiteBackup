using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class ExportCompleteArgs
    {
        DateTime _timeStart, _timeEnd;
        TimeSpan _timeUsed = new TimeSpan();
        Exception _exception;

        SQLiteBackup.ProcessEndType _completionType = SQLiteBackup.ProcessEndType.UnknownStatus;

        /// <summary>
        /// The Starting time of export process.
        /// </summary>
        public DateTime TimeStart { get { return _timeStart; } }

        /// <summary>
        /// The Ending time of export process.
        /// </summary>
        public DateTime TimeEnd { get { return _timeEnd; } }

        /// <summary>
        /// Total time used in current export process.
        /// </summary>
        public TimeSpan TimeUsed { get { return _timeUsed;}}

        public SQLiteBackup.ProcessEndType CompletionType { get { return _completionType; } }

        public Exception LastError { get { return _exception; } }

        public bool HasError { get { if (LastError != null) return true; return false; } }
        
        public ExportCompleteArgs(DateTime timeStart, DateTime timeEnd, SQLiteBackup.ProcessEndType endType, Exception exception)
        {
            _completionType = endType;
            _timeStart = timeStart;
            _timeEnd = timeEnd;
            _timeUsed = timeStart - timeEnd;
            _exception = exception;
        }
    }
}
