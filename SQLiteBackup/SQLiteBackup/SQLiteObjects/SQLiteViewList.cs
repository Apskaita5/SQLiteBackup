using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace System.Data.SQLite
{
    public class SQLiteViewList : IDisposable
    {
        List<SQLiteView> _lst = new List<SQLiteView>();
        string _sqlShowViewList = "";

        public string SqlShowViewList { get { return _sqlShowViewList; } }


        public SQLiteViewList()
        { }

        public SQLiteViewList(SQLiteCommand cmd, List<string> tables)
        {
            _sqlShowViewList = "SELECT name FROM sqlite_master WHERE type='view';";
                DataTable dt = QueryExpress.GetTable(cmd, _sqlShowViewList);

            foreach (DataRow dr in dt.Rows)
            {
               _lst.Add(new SQLiteView(cmd, dr[0] + ""));
            }

            SetDependancies(tables);

        }


        public SQLiteView this[int viewIndex]
        {
            get
            {
                return _lst[viewIndex];
            }
        }

        public SQLiteView this[string viewName]
        {
            get
            {
                for (int i = 0; i < _lst.Count; i++)
                {
                    if (_lst[i].Name == viewName)
                        return _lst[i];
                }
                throw new Exception(string.Format("View \"{0}\" is not existed.", viewName));
            }
        }


        public int Count
        {
            get
            {
                return _lst.Count;
            }
        }

        public bool Contains(string viewName)
        {
            if (this[viewName] == null)
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

        void SetDependancies(List<string> tables)
        {
            var list = this.ToList();
            foreach (var item in this)
            {
                item.SetDependancies(tables, list);
            }
        }


        public IEnumerator<SQLiteView> GetEnumerator()
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
