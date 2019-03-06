using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Data;

namespace System.Data.SQLite
{
    public class SQLiteDatabase : IDisposable
    {
        string _name = "";
        SQLiteTableList _listTable = new SQLiteTableList();
        SQLiteSequenceList _listSequence = new SQLiteSequenceList();
        SQLiteIndexList _listIndex = new SQLiteIndexList();
        SQLiteViewList _listView = new SQLiteViewList();
        SQLiteTriggerList _listTrigger = new SQLiteTriggerList();

        long _totalRows = 0;

        public string Name { get { return _name; } }
        public SQLiteTableList Tables { get { return _listTable; } }
        public SQLiteSequenceList Sequences { get { return _listSequence; } }
        public SQLiteIndexList Indexes { get { return _listIndex; } }
        public SQLiteViewList Views { get { return _listView; } }
        public SQLiteTriggerList Triggers { get { return _listTrigger; } }
        public long TotalRows { get { return _totalRows; } }

        public delegate void getTotalRowsProgressChange(object sender, GetTotalRowsArgs e);
        public event getTotalRowsProgressChange GetTotalRowsProgressChanged;


        internal SQLiteDatabase()
        { }

        public SQLiteDatabase(SQLiteCommand cmd, bool getTotalRowsForEachTable)
        {

            _name = cmd.Connection.FileName;

            _listTable = new SQLiteTableList(cmd);
            _listSequence = new SQLiteSequenceList(cmd);
            _listIndex = new SQLiteIndexList(cmd);
            var tables = _listTable.ToList();
            _listView = new SQLiteViewList(cmd, tables);
            _listTrigger = new SQLiteTriggerList(cmd, tables, _listView.ToList());

            if (getTotalRowsForEachTable)
                GetTotalRows(cmd);

        }

        void GetTotalRows(SQLiteCommand cmd)
        {

            int currentTableIndex = 0;

            foreach (var tbl in _listTable)
            {

                tbl.SetTotalRows(cmd);

                _totalRows += tbl.TotalRows;   

                if (GetTotalRowsProgressChanged != null)
                {
                    currentTableIndex += 1;
                    GetTotalRowsProgressChanged(this, new GetTotalRowsArgs(_listTable.Count, currentTableIndex));
                }

            }
                
        }

        public void Dispose()
        {
            _listTable.Dispose();
            _listSequence.Dispose();
            _listIndex.Dispose();
            _listTrigger.Dispose();
            _listView.Dispose();
        }
    }
}
