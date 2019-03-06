using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace System.Data.SQLite
{
    public class SQLiteColumnList : IDisposable
    {
        string _tableName;
        List<SQLiteColumn> _lst = new List<SQLiteColumn>();
        string _sqlShowFullColumns = "";

        public string SqlShowFullColumns { get { return _sqlShowFullColumns; } }

        public SQLiteColumnList()
        { }

        public SQLiteColumnList(SQLiteCommand cmd, string tableName)
        {
            _tableName = tableName;
            DataTable dtDataType = QueryExpress.GetTable(cmd, string.Format("SELECT * FROM  `{0}` where 1 = 2;", tableName));
            
            _sqlShowFullColumns = string.Format("PRAGMA table_info(`{0}`);", tableName);
            DataTable dtColInfo = QueryExpress.GetTable(cmd, _sqlShowFullColumns);

            for (int i = 0; i < dtDataType.Columns.Count; i++)
            {
                _lst.Add(new SQLiteColumn(
                    dtDataType.Columns[i].ColumnName,
                    dtDataType.Columns[i].DataType,
                    dtColInfo.Rows[i]["type"] + "",
                    ((Int64)dtColInfo.Rows[i]["notnull"] < 1),
                    dtColInfo.Rows[i]["dflt_value"] + "",
                    ((Int64)dtColInfo.Rows[i]["pk"] > 0)));
            }
        }

        public SQLiteColumn this[int columnIndex]
        {
            get
            {
                return _lst[columnIndex];
            }
        }

        public SQLiteColumn this[string columnName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].Name == columnName)
                    {
                        return _lst[i];
                    }
                }
                throw new Exception(string.Format("Column \"{0}\" does not exist in table \"{1}\".", 
                    columnName, _tableName));
            }
        }

        public int Count
        {
            get
            {
                return _lst.Count;
            }
        }

        public bool Contains(string columnName)
        {
            if (this[columnName] == null)
                return false;
            return true;
        }

        public void Dispose()
        {
            for (int i = 0; i < _lst.Count; i++)
            {
                _lst[i] = null;
            }
            _lst = null;
        }

        public IEnumerator<SQLiteColumn> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }

    }
}
