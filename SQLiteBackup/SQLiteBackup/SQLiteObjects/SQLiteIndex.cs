using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteIndex
    {

        string _name = "";
        string _createIndexSQL = "";
        string _tableName = "";


        public string Name { get { return _name; } }
        public string CreateIndexSQL { get { return _createIndexSQL; } }
        public string TableName { get { return _tableName; } }


        internal SQLiteIndex(string name, string createIndexSql, string tableName)
        {
            _name = name;
            _createIndexSQL = createIndexSql.Replace(Environment.NewLine, "^~~~~~~^").
                Replace("\r", "^~~~~~~^").Replace("\n", "^~~~~~~^").Replace("^~~~~~~^", " ");
            if (!_createIndexSQL.Trim().EndsWith(";")) _createIndexSQL = _createIndexSQL + ";";
            _tableName = tableName;
        }


        internal void Export(List<string> tablesToExport, SQLiteBackup manager)
        {

            if (manager.stopProcess || tablesToExport.Count < 1 ||
                !tablesToExport.Contains(_tableName.Trim().ToLower())) return;

            manager.Export_WriteLine(_createIndexSQL);
            manager.textWriter.WriteLine();

        }

    }
}
