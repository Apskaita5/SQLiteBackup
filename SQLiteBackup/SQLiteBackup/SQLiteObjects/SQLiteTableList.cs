using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace System.Data.SQLite
{
    public class SQLiteTableList : IDisposable
    {
        List<SQLiteTable> _lst = new List<SQLiteTable>();
        string _sqlShowFullTables = "";

        public string SqlShowFullTables { get { return _sqlShowFullTables; } }


        public SQLiteTableList()
        { }

        public SQLiteTableList(SQLiteCommand cmd)
        {
            _sqlShowFullTables = "SELECT tbl_name FROM sqlite_master WHERE type='table';";
            DataTable dtTableList = QueryExpress.GetTable(cmd, _sqlShowFullTables);

            foreach (DataRow dr in dtTableList.Rows)
            {
                if (dr[0] + "" != "sqlite_sequence") _lst.Add(new SQLiteTable(cmd, dr[0] + ""));
            }

            SetDependancies();

        }


        public SQLiteTable this[int tableIndex]
        {
            get 
            {
                return _lst[tableIndex];
            }
        }

        public SQLiteTable this[string tableName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].Name.ToLower().Trim() == tableName.ToLower().Trim())
                        return _lst[i];
                }
                throw new Exception(string.Format("Table \"{0}\" does not exist.", tableName));
            }
        }


        public int Count
        {
            get
            {
                return _lst.Count;
            }
        }

        public int IndexOf(SQLiteTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            for (int i = 0; i < _lst.Count; i++)
            {
                if (_lst[i].Name == table.Name)
                    return i;
            }
            return -1;
        }

        public List<string> ToList()
        {
            var result = new List<string>();
            foreach (var item in this)
            {
                result.Add(item.Name);
            }
            return result;
        }

        void SetDependancies()
        {
            var list = this.ToList();
            foreach (var item in this)
            {
                item.SetDependancies(list);
            }
        }


        public IEnumerator<SQLiteTable> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }

        public void Dispose()
        {
            for (int i = 0; i < _lst.Count; i++)
            {
                _lst[i].Dispose();
                _lst[i] = null;
            }
            _lst = null;
        }

    }
}
