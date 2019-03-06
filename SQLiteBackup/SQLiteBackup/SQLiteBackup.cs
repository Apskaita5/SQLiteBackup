using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;

namespace System.Data.SQLite
{
    public class SQLiteBackup
    {

        enum ProcessType
        {
            Export,
            Import
        }

        public enum ProcessEndType
        {
            UnknownStatus,
            Complete,
            Cancelled,
            Error
        }

        public const string Version = "1.0";

        SQLiteDatabase _database = new SQLiteDatabase();

        Encoding utf8WithoutBOM;
        internal TextWriter textWriter;
        TextReader textReader;
        DateTime timeStart;
        DateTime timeEnd;
        ProcessType currentProcess;

        string sha512HashedPassword = "";
        ProcessEndType processCompletionType;
        internal bool stopProcess = false;
        Exception _lastError = null;
        List<string> exportedViews;

        internal string _currentTableName = "";
        internal int _totalRowsInCurrentTable = 0;
        long _totalRowsInAllTables = 0;
        internal long _currentRowIndexInCurrentTable = 0;
        internal long _currentRowIndexInAllTable = 0;
        int _totalTables = 0;
        internal int _currentTableIndex = 0;
        Timer timerReport = null;

        long _currentBytes = 0L;
        long _totalBytes = 0L;
                
        bool _isNewDatabase = false;
        
        public Exception LastError { get { return _lastError; } }

        /// <summary>
        /// Gets the information about the connected database.
        /// </summary>
        public SQLiteDatabase Database { get { return _database; } }

        /// <summary>
        /// Gets or Sets the instance of MySqlCommand.
        /// </summary>
        public SQLiteCommand Command { get; set; }

        public ExportInformations ExportInfo = new ExportInformations();
        public ImportInformations ImportInfo = new ImportInformations();

        public delegate void exportProgressChange(object sender, ExportProgressArgs e);
        public event exportProgressChange ExportProgressChanged;

        public delegate void exportComplete(object sender, ExportCompleteArgs e);
        public event exportComplete ExportCompleted;

        public delegate void importProgressChange(object sender, ImportProgressArgs e);
        public event importProgressChange ImportProgressChanged;

        public delegate void importComplete(object sender, ImportCompleteArgs e);
        public event importComplete ImportCompleted;

        public delegate void getTotalRowsProgressChange(object sender, GetTotalRowsArgs e);
        public event getTotalRowsProgressChange GetTotalRowsProgressChanged;


        public SQLiteBackup()
        {
            InitializeComponents();
        }

        public SQLiteBackup(SQLiteCommand cmd)
        {
            InitializeComponents();
            Command = cmd;
        }

        void InitializeComponents()
        {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            _database.GetTotalRowsProgressChanged += _database_GetTotalRowsProgressChanged;

            timerReport = new Timer();
            timerReport.Elapsed += timerReport_Elapsed;

            utf8WithoutBOM = new UTF8Encoding(false);
        }

        //private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    System.IO.File.WriteAllText("mysql_error_log.txt", "hi :)");
        //    AssemblyName asname = new AssemblyName(args.Name);
        //    if (asname.Name == "MySql.Data")
        //    {
        //        return Assembly.LoadFile("MySql.Data.dll");
        //    }
        //    return null;
        //}

        void _database_GetTotalRowsProgressChanged(object sender, GetTotalRowsArgs e)
        {
            if (GetTotalRowsProgressChanged != null)
            {
                GetTotalRowsProgressChanged(this, e);
            }
        }


        #region Export

        public string ExportToString()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ExportToMemoryStream(ms);
                ms.Position = 0L;
                using (var thisReader = new StreamReader(ms))
                {
                    return thisReader.ReadToEnd();
                }
            }
        }

        public void ExportToFile(string filePath)
        {
            using (textWriter = new StreamWriter(filePath, false, utf8WithoutBOM))
            {
                ExportStart();
                textWriter.Close();
            }
        }

        public void ExportToTextWriter(TextWriter tw)
        {
            textWriter = tw;
            ExportStart();
        }

        public void ExportToMemoryStream(MemoryStream ms)
        {
            ExportToMemoryStream(ms, true);
        }

        public void ExportToMemoryStream(MemoryStream ms, bool resetMemoryStreamPosition)
        {
            if (resetMemoryStreamPosition)
            {
                if (ms == null)
                    ms = new MemoryStream();
                if (ms.Length > 0)
                    ms = new MemoryStream();
                ms.Position = 0L;
            }

            textWriter = new StreamWriter(ms, utf8WithoutBOM);
            ExportStart();
        }


        void ExportStart()
        {
            try
            {

                Export_InitializeVariables();

                int stage = 1;

                while (stage < 9)
                {
                    if (stopProcess) break;

                    switch (stage)
                    {
                        case 1: Export_BasicInfo(); break;
                        case 2: Export_DocumentHeader(); break;
                        case 3: Export_TableRows(); break;
                        case 4: Export_Views(); break;
                        case 5: Export_Triggers(); break;
                        case 6: Export_Sequencies(); break;
                        case 7: Export_Indexes(); break;
                        case 8: Export_DocumentFooter(); break;
                        default: break;
                    }

                    textWriter.Flush();

                    stage += 1;

                }

                if (stopProcess) processCompletionType = ProcessEndType.Cancelled;
                else processCompletionType = ProcessEndType.Complete;

            }
            catch (Exception ex)
            {
                _lastError = ex;
                processCompletionType = ProcessEndType.Error;
                StopAllProcess();
                throw;
            }

            ReportEndProcess();

        }

        void Export_InitializeVariables()
        {
            if (Command == null)
            {
                throw new Exception("SQLiteCommand is not initialized. Object not set to an instance of an object.");
            }

            if (Command.Connection == null)
            {
                throw new Exception("SQLiteCommand.Connection is not initialized. Object not set to an instance of an object.");
            }

            if (Command.Connection.State != System.Data.ConnectionState.Open)
            {
                throw new Exception("SQLiteCommand.Connection is not opened.");
            }

            timeStart = DateTime.Now;

            stopProcess = false;
            processCompletionType = ProcessEndType.UnknownStatus;
            currentProcess = ProcessType.Export;
            _lastError = null;
            timerReport.Interval = ExportInfo.IntervalForProgressReport;
            GetSHA512HashFromPassword(ExportInfo.EncryptionPassword);

            _database = new SQLiteDatabase(Command, ExportInfo.GetTotalRowsBeforeExport);
            _currentTableName = "";
            _totalRowsInCurrentTable = 0;
            _totalRowsInAllTables = _database.TotalRows;
            _currentRowIndexInCurrentTable = 0;
            _currentRowIndexInAllTable = 0;
            _totalTables = 0;
            _currentTableIndex = 0;

        }


        void Export_BasicInfo()
        {

            Export_WriteComment(string.Format("SQLiteBackup.NET {0}", SQLiteBackup.Version));

            if (ExportInfo.RecordDumpTime)
                Export_WriteComment(string.Format("Dump Time: {0}", timeStart.ToString("yyyy-MM-dd HH:mm:ss")));
            else
                Export_WriteComment("");
            Export_WriteComment(string.Format("Dump schema {0}", System.IO.Path.GetFileNameWithoutExtension(_database.Name)));
            Export_WriteComment("--------------------------------------");

            textWriter.WriteLine();

        }

        void Export_DocumentHeader()
        {
            textWriter.WriteLine();

            List<string> lstHeaders = ExportInfo.GetDocumentHeaders();
            if (lstHeaders.Count > 0)
            {
                foreach (string s in lstHeaders)
                {
                    Export_WriteLine(s);
                }

                textWriter.WriteLine();
                textWriter.WriteLine();
            }
        }

        void Export_TableRows()
        {
            List<string> tablesToExport = Export_GetTablesToBeExported();

            _totalTables = tablesToExport.Count;

            if (ExportInfo.ExportTableStructure || ExportInfo.ExportRows)
            {
                if (ExportProgressChanged != null)
                    timerReport.Start();

                foreach (var table in _database.Tables)
                {
                    if (stopProcess)
                        return;

                    table.Export(tablesToExport, _database.Tables, this);
                }
            }
        }

        bool Export_ThisTableIsExcluded(string tableName)
        {
            string tableNameLower = tableName.ToLower().Trim();

            foreach (string blacklistedTable in ExportInfo.ExcludeTables)
            {
                if (blacklistedTable.ToLower().Trim() == tableNameLower)
                    return true;
            }

            return false;
        }

        List<string> Export_GetTablesToBeExported()
        {
            var result = new List<string>();

            if (ExportInfo.TablesToBeExportedDic == null || ExportInfo.TablesToBeExportedDic.Count == 0)
            {
                foreach (SQLiteTable table in _database.Tables)
                {
                    if (!Export_ThisTableIsExcluded(table.Name)) result.Add(table.Name.Trim().ToLower());
                }
            }
            else
            {
                foreach (KeyValuePair<string, string> kv in ExportInfo.TablesToBeExportedDic)
                {
                    result.Add(kv.Key.Trim().ToLower());
                }
            }

            return result;
        }

        void Export_Views()
        {
            if (!ExportInfo.ExportViews || _database.Views.Count == 0)
                return;

            var tablesToExport = Export_GetTablesToBeExported();

            if (tablesToExport.Count < 1) return;

            Export_WriteComment("");
            Export_WriteComment("Dumping views");
            Export_WriteComment("");
            textWriter.WriteLine();

            exportedViews = new List<string>();

            foreach (SQLiteView view in _database.Views)
            {
                if (stopProcess)
                    return;

                view.Export(tablesToExport, _database.Views, this, exportedViews);
                
            }

            textWriter.WriteLine();
            textWriter.Flush();
        }

        void Export_Triggers()
        {
            if (!ExportInfo.ExportTriggers || _database.Triggers.Count == 0)
                return;

            var tablesToExport = Export_GetTablesToBeExported();

            if (tablesToExport.Count < 1) return;

            Export_WriteComment("");
            Export_WriteComment("Dumping triggers");
            Export_WriteComment("");
            textWriter.WriteLine();

            foreach (SQLiteTrigger trigger in _database.Triggers)
            {
                if (stopProcess)
                    return;

                trigger.Export(tablesToExport, exportedViews, this);

            }

            textWriter.Flush();
        }

        void Export_Sequencies()
        {
            if (!ExportInfo.ExportRows)
                return;

            var tablesToExport = Export_GetTablesToBeExported();

            if (tablesToExport.Count < 1) return;

            Export_WriteComment("");
            Export_WriteComment("Dumping sqlite_sequence");
            Export_WriteComment("");
            textWriter.WriteLine();

            Export_WriteLine("DELETE FROM sqlite_sequence;");

            foreach (var sequence in _database.Sequences)
            {
                if (stopProcess)
                    return;

                sequence.Export(tablesToExport, this);

            }

            textWriter.Flush();
        }

        void Export_Indexes()
        {
            if (!ExportInfo.ExportTableStructure)
                return;

            var tablesToExport = Export_GetTablesToBeExported();

            if (tablesToExport.Count < 1) return;

            Export_WriteComment("");
            Export_WriteComment("Dumping indexes");
            Export_WriteComment("");
            textWriter.WriteLine();

            foreach (var index in _database.Indexes)
            { 
                if (stopProcess) return;
                index.Export(tablesToExport, this);
            }

            textWriter.Flush();

        }

        void Export_DocumentFooter()
        {
            textWriter.WriteLine();

            List<string> lstFooters = ExportInfo.GetDocumentFooters();
            if (lstFooters.Count > 0)
            {
                foreach (string s in lstFooters)
                {
                    Export_WriteLine(s);
                }
            }

            timeEnd = DateTime.Now;

            if (ExportInfo.RecordDumpTime)
            {
                TimeSpan ts = timeEnd - timeStart;

                textWriter.WriteLine();
                textWriter.WriteLine();
                Export_WriteComment(string.Format("Dump completed on {0}", timeEnd.ToString("yyyy-MM-dd HH:mm:ss")));
                Export_WriteComment(string.Format("Total time: {0}:{1}:{2}:{3}:{4} (d:h:m:s:ms)", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
            }

            textWriter.Flush();
        }


        internal void Export_WriteComment(string text)
        {
            Export_WriteLine(string.Format("-- {0}", text));
        }

        internal void Export_WriteLine(string text)
        {
            if (ExportInfo.EnableEncryption)
            {
                textWriter.WriteLine(Encrypt(text));
            }
            else
            {
                textWriter.WriteLine(text);
            }
        }

        #endregion

        #region Import

        public void ImportFromString(string sqldumptext)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter thisWriter = new StreamWriter(ms))
                {
                    thisWriter.Write(sqldumptext);
                    thisWriter.Flush();

                    ms.Position = 0L;

                    ImportFromMemoryStream(ms);
                }
            }
        }

        public void ImportFromFile(string filePath)
        {
            System.IO.FileInfo fi = new FileInfo(filePath);

            using (TextReader tr = new StreamReader(filePath))
            {
                ImportFromTextReaderStream(tr, fi);
            }
        }

        public void ImportFromTextReader(TextReader tr)
        {
            ImportFromTextReaderStream(tr, null);
        }

        public void ImportFromMemoryStream(MemoryStream ms)
        {
            ms.Position = 0;
            _totalBytes = ms.Length;
            textReader = new StreamReader(ms);
            Import_Start();
        }

        void ImportFromTextReaderStream(TextReader tr, FileInfo fileInfo)
        {
            if (fileInfo != null)
                _totalBytes = fileInfo.Length;
            else
                _totalBytes = 0L;

            textReader = tr;

            Import_Start();
        }


        void Import_Start()
        {
            Import_InitializeVariables();

            string line = "";
            int lineCount = 0;
            var statement = new StringBuilder();

            while (line != null)
            {
                if (stopProcess)
                {
                    processCompletionType = ProcessEndType.Cancelled;
                    break;
                }

                try
                {
                    line = Import_GetLine();

                    if (!Import_IsEmptyLine(line))
                    {
                        statement.AppendLine(line);
                        if (line.Trim().EndsWith(";"))
                        {
                            Command.CommandText = statement.ToString();
                            Command.ExecuteNonQuery();
                            statement = new StringBuilder();
                        }                        
                    }

                    if (lineCount > 5000)
                    {
                        lineCount = 0;
                        GC.Collect();
                    }
                    else
                    {
                        lineCount += 1;
                    }

                }
                catch (Exception ex)
                {
                    line = "";

                    _lastError = ex;
                    if (ImportInfo.IgnoreSqlError)
                    {
                        if (!string.IsNullOrEmpty(ImportInfo.ErrorLogFile))
                        {
                            File.AppendAllText(ImportInfo.ErrorLogFile, Environment.NewLine + Environment.NewLine + ex.ToString());
                        }
                    }
                    else
                    {
                        StopAllProcess();
                        throw;
                    }
                }
            }

            ReportEndProcess();
        }

        void Import_InitializeVariables()
        {
            if (Command == null)
            {
                throw new Exception("SQLiteCommand is not initialized. Object not set to an instance of an object.");
            }

            if (Command.Connection == null)
            {
                throw new Exception("SQLiteCommand.Connection is not initialized. Object not set to an instance of an object.");
            }

            if (Command.Connection.State != System.Data.ConnectionState.Open)
            {
                throw new Exception("SQLiteCommand.Connection is not opened.");
            }

            stopProcess = false;
            GetSHA512HashFromPassword(ImportInfo.EncryptionPassword);
            _lastError = null;
            timeStart = DateTime.Now;
            _currentBytes = 0L;
            currentProcess = ProcessType.Import;
            processCompletionType = ProcessEndType.Complete;

            if (ImportProgressChanged != null)
                timerReport.Start();

        }

        string Import_GetLine()
        {
            string line = textReader.ReadLine();

            if (line == null)
                return null;

            if (ImportProgressChanged != null)
            {
                _currentBytes = _currentBytes + (long)line.Length;
            }

            if (Import_IsEmptyLine(line))
            {
                return string.Empty;
            }

            line = line.Trim();

            if (!ImportInfo.EnableEncryption)
                return line;

            line = Decrypt(line);

            return line.Trim();
        }

        bool Import_IsEmptyLine(string line)
        {
            if (line == null)
                return true;
            if (line == string.Empty)
                return true;
            if (line.Trim().Length == 0)
                return true;
            if (line.StartsWith("--"))
                return true;
            if (line == Environment.NewLine)
                return true;
            if (line == "\r")
                return true;
            if (line == "\n")
                return true;

            return false;
        }

        #endregion

        #region Encryption

        void GetSHA512HashFromPassword(string password)
        {
            sha512HashedPassword = CryptoExpress.Sha512Hash(password);
        }

        string Encrypt(string text)
        {
            return CryptoExpress.AES_Encrypt(text, sha512HashedPassword);
        }

        string Decrypt(string text)
        {
            return CryptoExpress.AES_Decrypt(text, sha512HashedPassword);
        }

        public void EncryptDumpFile(string sourceFile, string outputFile, string password)
        {
            using (TextReader trSource = new StreamReader(sourceFile))
            {
                using (TextWriter twOutput = new StreamWriter(outputFile, false, utf8WithoutBOM))
                {
                    EncryptDumpFile(trSource, twOutput, password);
                    twOutput.Close();
                }
                trSource.Close();
            }
        }

        public void EncryptDumpFile(TextReader trSource, TextWriter twOutput, string password)
        {
            GetSHA512HashFromPassword(password);

            string line = trSource.ReadLine();

            while (line != null)
            {
                
                line = Encrypt(line);

                twOutput.WriteLine(line);
                twOutput.Flush();

                line = trSource.ReadLine();

            }

        }

        public void DecryptDumpFile(string sourceFile, string outputFile, string password)
        {
            using (TextReader trSource = new StreamReader(sourceFile))
            {
                using (TextWriter twOutput = new StreamWriter(outputFile, false, utf8WithoutBOM))
                {
                    DecryptDumpFile(trSource, twOutput, password);
                    twOutput.Close();
                }
                trSource.Close();
            }
        }

        public void DecryptDumpFile(TextReader trSource, TextWriter twOutput, string password)
        {
            GetSHA512HashFromPassword(password);

            string line = trSource.ReadLine();

            while (line != null)
            {
                
                if (line.Trim().Length == 0)
                {
                    twOutput.WriteLine();
                }

                line = Decrypt(line);

                twOutput.WriteLine(line);
                twOutput.Flush();

                line = trSource.ReadLine();

            }
        }

        #endregion

        void ReportEndProcess()
        {
            timeEnd = DateTime.Now;

            StopAllProcess();

            if (currentProcess == ProcessType.Export)
            {
                ReportProgress();
                if (ExportCompleted != null)
                {
                    ExportCompleted(this, new ExportCompleteArgs(timeStart, timeEnd, processCompletionType, _lastError));
                }
            }
            else if (currentProcess == ProcessType.Import)
            {
                _currentBytes = _totalBytes;

                ReportProgress();
                if (ImportCompleted != null)
                {
                    ImportCompleted(this, new ImportCompleteArgs());
                }
            }
        }

        void timerReport_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReportProgress();
        }

        void ReportProgress()
        {
            if (currentProcess == ProcessType.Export)
            {
                if (ExportProgressChanged != null)
                {
                    ExportProgressChanged(this, new ExportProgressArgs(_currentTableName, 
                        _totalRowsInCurrentTable, _totalRowsInAllTables, _currentRowIndexInCurrentTable, 
                        _currentRowIndexInAllTable, _totalTables, _currentTableIndex));
                }
            }
            else if (currentProcess == ProcessType.Import)
            {
                if (ImportProgressChanged != null)
                {
                    ImportProgressChanged(this, new ImportProgressArgs(_currentBytes, _totalBytes));
                }
            }
        }

        public void StopAllProcess()
        {
            stopProcess = true;
            timerReport.Stop();
        }

        public void Dispose()
        {
            try
            {
                _database.Dispose();
            }
            catch { }
        }

    }
}
