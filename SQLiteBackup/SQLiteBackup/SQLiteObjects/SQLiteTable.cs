using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace System.Data.SQLite
{
    public class SQLiteTable : IDisposable
    {
        string _name = "";
        SQLiteColumnList _lst = null;
        int _totalRows = 0;
        string _createTableSql = "";
        string _insertStatementHeader = "";
        string _insertStatementHeaderWithoutColumns = "";
        List<string> _dependancies = new List<string>();
        bool _isProcessed = false;


        public string Name { get { return _name; } }
        public int TotalRows { get { return _totalRows; } }
        public string CreateTableSql { get { return _createTableSql; } }
        public SQLiteColumnList Columns { get { return _lst; } }
        public string InsertStatementHeaderWithoutColumns { get { return _insertStatementHeaderWithoutColumns; } }
        public string InsertStatementHeader { get { return _insertStatementHeader; } }
        public List<string> Dependancies { get { return _dependancies; } }
        public bool IsProcessed { get { return _isProcessed; } private set { _isProcessed = value;  } }


        public SQLiteTable(SQLiteCommand cmd, string name)
        {
            _name = name;
            string sql = string.Format("SELECT sql FROM sqlite_master WHERE type='table' AND tbl_name='{0}';", name);
            _createTableSql = QueryExpress.ExecuteScalarStr(cmd, sql, 0).Replace(Environment.NewLine, "^~~~~~~^").
                Replace("\r", "^~~~~~~^").Replace("\n", "^~~~~~~^").Replace("^~~~~~~^", " ").
                Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ") + ";";
            _lst = new SQLiteColumnList(cmd, name);
            GetInsertStatementHeaders();
        }


        void GetInsertStatementHeaders()
        {
            _insertStatementHeaderWithoutColumns = string.Format("INSERT INTO `{0}` VALUES", _name);

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO `");
            sb.Append(_name);
            sb.Append("` (");
            for (int i = 0; i < _lst.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append("`");
                sb.Append(_lst[i].Name);
                sb.Append("`");
            }
            sb.Append(") VALUES");

            _insertStatementHeader = sb.ToString();
        }

        internal void SetTotalRows(SQLiteCommand cmd)
        {
            string sql = string.Format("SELECT COUNT(*) FROM `{0}`;", _name);
            _totalRows = (int)QueryExpress.ExecuteScalarLong(cmd, sql);
        }

        internal void SetDependancies(List<string> tables)
        {
            if (_createTableSql.ToLower().Contains("references "))
            _dependancies = QueryExpress.GetDependancies(_createTableSql, tables);
        }


        internal void Export(List<string> tablesToExport, SQLiteTableList allTables, 
            SQLiteBackup manager)
        {

            if (_isProcessed || manager.stopProcess || !tablesToExport.Contains(_name.Trim().ToLower())) return;

            if (manager.ExportInfo.ExportTableStructure)
            {

                if (_dependancies.Count > 0)
                {
                    foreach (var dependantTable in _dependancies)
                    {
                        if (!tablesToExport.Contains(dependantTable))
                            throw new Exception(string.Format("Table \"{0}\" depends on table \"{1}\" via foreign key. However the latter is not included within exported table list.",
                                _name, dependantTable));
                    }
                    foreach (var dependantTable in _dependancies)
                    {
                        allTables[dependantTable].Export(tablesToExport, allTables, manager);
                    }
                }

                manager._currentTableName = _name;
                manager._currentTableIndex = allTables.IndexOf(this);
                manager._totalRowsInCurrentTable = _totalRows;

                manager.Export_WriteComment("");
                manager.Export_WriteComment(string.Format("Definition of {0}", _name));
                manager.Export_WriteComment("");

                manager.textWriter.WriteLine();

                manager.Export_WriteLine(string.Format("DROP TABLE IF EXISTS `{0}`;", _name));

                manager.Export_WriteLine(_createTableSql);

                manager.textWriter.WriteLine();

                manager.textWriter.Flush();

            }

            if (manager.ExportInfo.ExportRows)
            {

                manager.Export_WriteComment("");
                manager.Export_WriteComment(string.Format("Dumping data for table {0}", _name));
                manager.Export_WriteComment("");
                manager.textWriter.WriteLine();

                manager._currentRowIndexInCurrentTable = 0L;

                if (manager.ExportInfo.RowsExportMode == RowsDataExportMode.Insert ||
                    manager.ExportInfo.RowsExportMode == RowsDataExportMode.InsertIgnore ||
                    manager.ExportInfo.RowsExportMode == RowsDataExportMode.Replace)
                {
                    Export_RowsData_Insert_Ignore_Replace(manager);
                }
                else if (manager.ExportInfo.RowsExportMode == RowsDataExportMode.OnDuplicateKeyUpdate)
                {
                    Export_RowsData_OnDuplicateKeyUpdate(manager);
                }
                else if (manager.ExportInfo.RowsExportMode == RowsDataExportMode.Update)
                {
                    Export_RowsData_Update(manager);
                }

                manager.textWriter.WriteLine();
                manager.textWriter.Flush();

            }

            _isProcessed = true;

        }

        void Export_RowsData_Insert_Ignore_Replace(SQLiteBackup manager)
        {

            manager.Command.CommandText = string.Format("SELECT * FROM `{0}`;", _name);
            SQLiteDataReader rdr = manager.Command.ExecuteReader();

            var sb = new StringBuilder((int)manager.ExportInfo.MaxSqlLength);

            manager._currentRowIndexInCurrentTable = 0;
            _insertStatementHeader = string.Empty;

            while (rdr.Read())
            {
                if (manager.stopProcess)
                    return;

                manager._currentRowIndexInAllTable += 1;
                manager._currentRowIndexInCurrentTable += 1;

                if (_insertStatementHeader == string.Empty)
                    _insertStatementHeader = Export_GetInsertStatementHeader(
                        manager.ExportInfo.RowsExportMode, rdr);

                string sqlDataRow = Export_GetValueString(rdr);

                if (sb.Length == 0)
                {
                    sb.AppendLine(_insertStatementHeader);
                    sb.Append(sqlDataRow);
                }
                else if ((long)sb.Length + (long)sqlDataRow.Length < manager.ExportInfo.MaxSqlLength)
                {
                    sb.AppendLine(",");
                    sb.Append(sqlDataRow);
                }
                else
                {
                    sb.Append(";");

                    manager.Export_WriteLine(sb.ToString());
                    manager.textWriter.Flush();

                    sb = new StringBuilder((int)manager.ExportInfo.MaxSqlLength);
                    sb.AppendLine(_insertStatementHeader);
                    sb.Append(sqlDataRow);
                }
            }

            rdr.Close();

            if (sb.Length > 0)
            {
                sb.Append(";");
            }

            manager.Export_WriteLine(sb.ToString());
            manager.textWriter.Flush();

            sb = null;

        }

        void Export_RowsData_OnDuplicateKeyUpdate(SQLiteBackup manager)
        {
            bool allPrimaryField = true;
            foreach (var col in Columns)
            {
                if (!col.IsPrimaryKey)
                {
                    allPrimaryField = false;
                    break;
                }
            }

            manager.Command.CommandText = string.Format("SELECT * FROM `{0}`;", _name); 
            SQLiteDataReader rdr = manager.Command.ExecuteReader();

            _insertStatementHeader = string.Empty;

            while (rdr.Read())
            {
                if (manager.stopProcess)
                    return;

                if (_insertStatementHeader == string.Empty)
                {
                    if (allPrimaryField)
                    {
                        _insertStatementHeader = Export_GetInsertStatementHeader(RowsDataExportMode.InsertIgnore, rdr);
                    }
                    else
                    {
                        _insertStatementHeader = Export_GetInsertStatementHeader(RowsDataExportMode.Insert, rdr);
                    }
                }
                
                StringBuilder sb = new StringBuilder();

                sb.Append(_insertStatementHeader);
                sb.Append(Export_GetValueString(rdr));

                if (!allPrimaryField)
                {
                    sb.Append(" ON CONFLICT DO UPDATE SET ");
                    Export_GetUpdateString(rdr, sb);
                }

                sb.Append(";");

                manager.Export_WriteLine(sb.ToString());
                manager.textWriter.Flush();
            }

            rdr.Close();
        }

        void Export_RowsData_Update(SQLiteBackup manager)
        {

            bool allPrimaryField = true;
            foreach (var col in Columns)
            {
                if (!col.IsPrimaryKey)
                {
                    allPrimaryField = false;
                    break;
                }
            }

            if (allPrimaryField)
                return;

            bool allNonPrimaryField = true;
            foreach (var col in Columns)
            {
                if (col.IsPrimaryKey)
                {
                    allNonPrimaryField = false;
                    break;
                }
            }

            if (allNonPrimaryField)
                return;

            manager.Command.CommandText = string.Format("SELECT * FROM `{0}`;", _name);
            SQLiteDataReader rdr = manager.Command.ExecuteReader();

            while (rdr.Read())
            {
                if (manager.stopProcess)
                    return;

                StringBuilder sb = new StringBuilder();
                sb.Append("UPDATE `");
                sb.Append(_name);
                sb.Append("` SET ");

                Export_GetUpdateString(rdr, sb);

                sb.Append(" WHERE ");

                Export_GetConditionString(rdr, sb);

                sb.Append(";");

                manager.Export_WriteLine(sb.ToString());

                manager.textWriter.Flush();
            }

            rdr.Close();

        }

        string Export_GetInsertStatementHeader(RowsDataExportMode rowsExportMode, SQLiteDataReader rdr)
        {
            StringBuilder sb = new StringBuilder();

            if (rowsExportMode == RowsDataExportMode.Insert)
                sb.Append("INSERT INTO `");
            else if (rowsExportMode == RowsDataExportMode.InsertIgnore)
                sb.Append("INSERT OR IGNORE INTO `");
            else if (rowsExportMode == RowsDataExportMode.Replace)
                sb.Append("REPLACE INTO `");

            sb.Append(_name);
            sb.Append("`(");

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append("`");
                sb.Append(rdr.GetName(i));
                sb.Append("`");
            }

            sb.Append(") VALUES");
            return sb.ToString();

        }

        void Export_GetUpdateString(SQLiteDataReader rdr, StringBuilder sb)
        {
            bool isFirst = true;

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                string colName = rdr.GetName(i);

                var col = Columns[colName];

                if (!col.IsPrimaryKey)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        sb.Append(",");

                    sb.Append("`");
                    sb.Append(colName);
                    sb.Append("`=");
                    sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, col));
                }
            }

        }

        void Export_GetConditionString(SQLiteDataReader rdr, StringBuilder sb)
        {
            bool isFirst = true;

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                string colName = rdr.GetName(i);

                var col = Columns[colName];

                if (col.IsPrimaryKey)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        sb.Append(" AND ");

                    sb.Append("`");
                    sb.Append(colName);
                    sb.Append("`=");
                    sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, col));
                }
            }
        }

        string Export_GetValueString(SQLiteDataReader rdr)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < rdr.FieldCount; i++)
            {

                if (sb.Length == 0)
                    sb.AppendFormat("(");
                else
                    sb.AppendFormat(",");
                
                sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, Columns[rdr.GetName(i)]));

            }

            sb.AppendFormat(")");

            return sb.ToString();

        }


        public void Dispose()
        {
            _lst.Dispose();
            _lst = null;
        }

    }
}
