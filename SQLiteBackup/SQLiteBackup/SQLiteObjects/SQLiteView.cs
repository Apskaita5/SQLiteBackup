using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteView
    {
        string _name = "";
        string _createViewSQL = "";
        List<string> _tableDependancies = new List<string>();
        List<string> _viewDependancies = new List<string>();
        bool _isProcessed = false;

        public string Name { get { return _name; } }
        public string CreateViewSQL { get { return _createViewSQL; } }
        public List<string> TableDependancies { get { return _tableDependancies; } }
        public List<string> ViewDependancies { get { return _viewDependancies; } }
        public bool IsProcessed { get { return _isProcessed; } private set { _isProcessed = value; } }


        public SQLiteView(SQLiteCommand cmd, string viewName)
        {
            _name = viewName;

            string sqlShowCreate = string.Format("SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '{0}';", viewName);

            var dtView = QueryExpress.GetTable(cmd, sqlShowCreate);

            _createViewSQL = dtView.Rows[0][0] + ";";

            _createViewSQL = _createViewSQL.Replace("\r\n", "^~~~~~~~~~~~~~~^").
                Replace("\n", "^~~~~~~~~~~~~~~^").Replace("\r", "^~~~~~~~~~~~~~~^").
                Replace("^~~~~~~~~~~~~~~^", " ");

        }

        internal void SetDependancies(List<string> tables, List<string> views)
        {
            _tableDependancies = QueryExpress.GetDependancies(_createViewSQL, tables);
            _viewDependancies = QueryExpress.GetDependancies(_createViewSQL, views);
        }


        internal void Export(List<string> tablesToExport, SQLiteViewList allViews,
            SQLiteBackup manager, List<string> exportedViews)
        {

            if (_isProcessed || manager.stopProcess) return;

            foreach (var dependantTable in _tableDependancies)
                {
                    // skip views that are dependant on skiped tables
                    if (!tablesToExport.Contains(dependantTable))
                        return;
                }
            foreach (var dependantView in _viewDependancies)
                {
                    allViews[dependantView].Export(tablesToExport, allViews, manager, exportedViews);
                }


            _isProcessed = true;

            if (_createViewSQL.Trim().Length == 0) return;

            manager.Export_WriteLine(string.Format("DROP TABLE IF EXISTS `{0}`;", _name));
            manager.Export_WriteLine(string.Format("DROP VIEW IF EXISTS `{0}`;", _name));

            manager.Export_WriteLine(_createViewSQL);

            manager.textWriter.WriteLine();

            exportedViews.Add(_name);
                       
        }

    }
}
