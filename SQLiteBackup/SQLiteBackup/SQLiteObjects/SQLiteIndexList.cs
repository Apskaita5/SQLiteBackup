using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteIndexList : IDisposable
    {

        List<SQLiteIndex> _lst = new List<SQLiteIndex>();
        string _sqlShowIndexes = "";

        public string SqlShowIndexes { get { return _sqlShowIndexes; } }

        internal SQLiteIndexList()
        { }

        internal SQLiteIndexList(SQLiteCommand cmd)
        {
            _sqlShowIndexes = "SELECT name, sql, tbl_name FROM sqlite_master WHERE type = 'index' AND sql <> '';";

            DataTable dt = QueryExpress.GetTable(cmd, _sqlShowIndexes);

            foreach (DataRow dr in dt.Rows)
            {
                _lst.Add(new SQLiteIndex(dr[0] + "", dr[1] + "", dr[2] + ""));
            }

        }

        public SQLiteIndex this[int indexIndex]
        {
            get
            {
                return _lst[indexIndex];
            }
        }

        public SQLiteIndex this[string indexName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].Name == indexName)
                    {
                        return _lst[i];
                    }
                }
                throw new Exception(string.Format("Index \"{0}\" does not exist.", indexName));
            }
        }

        public int Count
        {
            get
            {
                return _lst.Count;
            }
        }

        public bool Contains(string indexName)
        {
            if (this[indexName] == null)
                return false;
            return true;
        }

        public IEnumerator<SQLiteIndex> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }

        public void Dispose()
        {
            for (int i = 0; i < _lst.Count; i++)
            {
                _lst[i] = null;
            }
            _lst = null;
        }

    }
}
