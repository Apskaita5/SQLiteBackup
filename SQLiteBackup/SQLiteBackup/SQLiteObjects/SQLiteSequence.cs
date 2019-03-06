using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteSequence
    {

        string _tableName = "";
        Int64 _value = 0;

        public string TableName => _tableName; 
        public Int64 Value => _value; 

        internal SQLiteSequence(string tableName, Int64 value)
        {
            _tableName = tableName;
            _value = value;

        }


        internal void Export(List<string> tablesToExport, SQLiteBackup manager)
        {

            if (manager.stopProcess || tablesToExport.Count < 1 || 
                !tablesToExport.Contains(_tableName.Trim().ToLower())) return;

            manager.Export_WriteLine(string.Format("INSERT INTO \"sqlite_sequence\" VALUES('{0}', {1});", 
                _tableName, _value.ToString()));
            manager.textWriter.WriteLine();

        }

    }

}
