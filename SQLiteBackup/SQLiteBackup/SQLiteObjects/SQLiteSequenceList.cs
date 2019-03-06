using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteSequenceList : IDisposable
    {

        List<SQLiteSequence> _lst = new List<SQLiteSequence>();
        string _sqlShowSequence = "";

        public string SqlShowSequence { get { return _sqlShowSequence; } }


        internal SQLiteSequenceList()
        {
        }

        public SQLiteSequenceList(SQLiteCommand cmd)
        {
            _sqlShowSequence = "SELECT name, seq FROM sqlite_sequence;";

            DataTable dt = QueryExpress.GetTable(cmd, _sqlShowSequence);

            foreach (DataRow dr in dt.Rows)
            {
                _lst.Add(new SQLiteSequence(dr[0] + "", (Int64)dr[1]));
            }

        }

        public SQLiteSequence this[int tableIndex]
        {
            get
            {
                return _lst[tableIndex];
            }
        }

        public SQLiteSequence this[string tableName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].TableName == tableName)
                    {
                        return _lst[i];
                    }
                }
                throw new Exception(string.Format("Sequence for table \"{0}\" does not exist.", tableName));
            }
        }

        public int Count
        {
            get
            {
                return _lst.Count;
            }
        }

        public bool Contains(string triggerName)
        {
            if (this[triggerName] == null)
                return false;
            return true;
        }

        public IEnumerator<SQLiteSequence> GetEnumerator()
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
