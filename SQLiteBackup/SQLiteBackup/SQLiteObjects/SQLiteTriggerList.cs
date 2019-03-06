using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace System.Data.SQLite
{
    public class SQLiteTriggerList : IDisposable
    {
        List<SQLiteTrigger> _lst = new List<SQLiteTrigger>();
        string _sqlShowTriggers = "";

       public string SqlShowTriggers { get { return _sqlShowTriggers; } }


        public SQLiteTriggerList()
        { }

        public SQLiteTriggerList(SQLiteCommand cmd, List<string> tables, List<string> views)
        {
            _sqlShowTriggers = "SELECT name FROM sqlite_master WHERE type='trigger';";

            DataTable dt = QueryExpress.GetTable(cmd, _sqlShowTriggers);

                foreach (DataRow dr in dt.Rows)
                {
                    _lst.Add(new SQLiteTrigger(cmd, dr[0] + ""));
                }

            SetDependancies(tables, views);

        }


        public SQLiteTrigger this[int triggerIndex]
        {
            get
            {
                return _lst[triggerIndex];
            }
        }

        public SQLiteTrigger this[string triggerName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].Name == triggerName)
                    {
                        return _lst[i];
                    }
                }
                throw new Exception(string.Format("Trigger \"{0}\" does not exist.", triggerName));
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

        public List<string> ToList()
        {
            var result = new List<string>();
            foreach (var item in this)
            {
                result.Add(item.Name);
            }
            return result;
        }

        void SetDependancies(List<string> tables, List<string> views)
        {
            foreach (var item in this)
            {
                item.SetDependancies(tables, views);
            }
        }


        public IEnumerator<SQLiteTrigger> GetEnumerator()
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
