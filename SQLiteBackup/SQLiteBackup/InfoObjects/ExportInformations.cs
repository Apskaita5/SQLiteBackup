using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    /// <summary>
    /// Informations and Settings of SQLite Database Export Process
    /// </summary>
    public class ExportInformations
    {
        int _interval = 50;

        List<string> _documentHeaders = null;
        List<string> _documentFooters = null;

        Dictionary<string, string> _customTable = new Dictionary<string, string>();

        List<string> _lstExcludeTables = null;
        
        /// <summary>
        /// Gets or Sets the tables (black list) that will be excluded for export. The rows of the these tables will not be exported too.
        /// </summary>
        public List<string> ExcludeTables
        {
            get
            {
                if (_lstExcludeTables == null)
                    _lstExcludeTables = new List<string>();
                return _lstExcludeTables;
            }
            set
            {
                _lstExcludeTables = value;
            }
        }

        /// <summary>
        /// Gets the list of document headers.
        /// </summary>
        /// <returns>List of document headers.</returns>
        public List<string> GetDocumentHeaders()
        {
            if (_documentHeaders == null)
            {
                _documentHeaders = new List<string>();
                
                _documentHeaders.Add("PRAGMA foreign_keys=OFF;");
                _documentHeaders.Add("BEGIN TRANSACTION;");
            }

            return _documentHeaders;
        }

        /// <summary>
        /// Sets the document headers.
        /// </summary>
        /// <param name="lstHeaders">List of document headers</param>
        public void SetDocumentHeaders(List<string> lstHeaders)
        {
            _documentHeaders = lstHeaders;
        }

        /// <summary>
        /// Gets the document footers.
        /// </summary>
        /// <returns>List of document footers.</returns>
        public List<string> GetDocumentFooters()
        {
            if (_documentFooters == null)
            {

                _documentFooters = new List<string>();

                _documentFooters.Add("COMMIT;");
                _documentFooters.Add("PRAGMA foreign_keys=ON;");
                
            }

            return _documentFooters;
        }

        /// <summary>
        /// Sets the document footers.
        /// </summary>
        /// <param name="lstFooters">List of document footers.</param>
        public void SetDocumentFooters(List<string> lstFooters)
        {
            _documentFooters = lstFooters;
        }

        /// <summary>
        /// Gets or Sets the list of tables that will be exported. If none, all tables will be exported.
        /// </summary>
        public List<string> TablesToBeExportedList
        {
            get
            {
                List<string> lst = new List<string>();
                foreach (KeyValuePair<string, string> kv in _customTable)
                {
                    lst.Add(kv.Key);
                }
                return lst;
            }
            set
            {
                _customTable.Clear();
                foreach (string s in value)
                {
                    _customTable[s] = string.Format("SELECT * FROM `{0}`;", s);
                }
            }
        }

        /// <summary>
        /// Gets or Sets the tables that will be exported with custom SELECT defined. If none or empty, all tables and rows will be exported. Key = Table's Name. Value = Custom SELECT Statement. Example 1: SELECT * FROM `product` WHERE `category` = 1; Example 2: SELECT `name`,`description` FROM `product`;
        /// </summary>
        public Dictionary<string, string> TablesToBeExportedDic
        {
            get
            {
                return _customTable;
            }
            set
            {
                _customTable = value;
            }
        }

        /// <summary>
        /// Gets or Sets a value indicates whether the Dump Time should recorded in dump file.
        /// </summary>
        public bool RecordDumpTime = true;

        /// <summary>
        /// Gets or Sets a value indicates whether the Exported Dump File should be encrypted. Enabling encryption will slow down the whole process.
        /// </summary>
        [System.Obsolete("This implementation will slow down the whole process which is not recommended. Encrypt the content externally after the export process completed. For more information, please read documentation.")]
        public bool EnableEncryption = false;

        /// <summary>
        /// Sets the password used to encrypt the exported dump file.
        /// </summary>
        [System.Obsolete("This implementation will slow down the whole process which is not recommended. Encrypt the content externally after the export process completed. For more information, please read documentation.")]
        public string EncryptionPassword = "";

        /// <summary>
        /// Gets or Sets a value indicates whether the Table Structure (CREATE TABLE) should be exported.
        /// </summary>
        public bool ExportTableStructure = true;

        /// <summary>
        /// Gets or Sets a value indicates whether the Rows should be exported.
        /// </summary>
        public bool ExportRows = true;

        /// <summary>
        /// Gets or Sets the maximum length for combining multiple INSERTs into single sql. Default value is 5MB. Only applies if RowsExportMode = "INSERT" or "INSERTIGNORE" or "REPLACE". This value will be ignored if RowsExportMode = ONDUPLICATEKEYUPDATE or UPDATE.
        /// </summary>
        public int MaxSqlLength = 5 * 1024 * 1024;

        /// <summary>
        /// Gets or Sets a value indicates whether the Stored Triggers should be exported.
        /// </summary>
        public bool ExportTriggers = true;

        /// <summary>
        /// Gets or Sets a value indicates whether the Stored Views should be exported.
        /// </summary>
        public bool ExportViews = true;

        /// <summary>
        /// Gets or Sets a value indicates the interval of time (in miliseconds) to raise the event of ExportProgressChanged.
        /// </summary>
        public int IntervalForProgressReport { get { if (_interval == 0) return 100; return _interval; } set { _interval = value; } }

        /// <summary>
        /// Gets or Sets a value indicates whether the totals of rows should be counted before export process commence. The value of total rows is used for progress reporting. Extra time is needed to get the total rows. Sets this value to FALSE if not applying progress reporting.
        /// </summary>
        public bool GetTotalRowsBeforeExport = true;

        /// <summary>
        /// Gets or Sets a enum value indicates how the rows of each table should be exported. INSERT = The default option. Recommended if exporting to a new database. If the primary key existed, the process will halt; INSERT IGNORE = If the primary key existed, skip it; REPLACE = If the primary key existed, delete the row and insert new data; OnDuplicateKeyUpdate = If the primary key existed, update the row. If all fields are primary keys, it will change to INSERT IGNORE; UPDATE = If the primary key is not existed, skip it and if all the fields are primary key, no rows will be exported.
        /// </summary>
        public RowsDataExportMode RowsExportMode = RowsDataExportMode.Insert;
                 

        public ExportInformations()
        {
        }

    }
}
