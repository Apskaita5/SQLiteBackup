using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.SQLite
{
    public class SQLiteTrigger
    {
        string _name = "";
        string _createTriggerSQL = "";
        List<string> _tableDependancies = new List<string>();
        List<string> _viewsDependancies = new List<string>();
        bool _isProcessed = false;

        public string Name { get { return _name; } }
        public string CreateTriggerSQL { get { return _createTriggerSQL; } }
        public List<string> TableDependancies { get { return _tableDependancies; } }
        public List<string> ViewsDependancies { get { return _viewsDependancies; } }
        public bool IsProcessed { get { return _isProcessed; } private set { _isProcessed = value; } }

        public SQLiteTrigger(SQLiteCommand cmd, string triggerName)
        {

            _name = triggerName;

            _createTriggerSQL = QueryExpress.ExecuteScalarStr(cmd, string.Format("SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{0}';", triggerName), 0);

            _createTriggerSQL = _createTriggerSQL.Replace("\r\n", "^~~~~~~~~~~~~~~^").
                Replace("\n", "^~~~~~~~~~~~~~~^").Replace("\r", "^~~~~~~~~~~~~~~^").
                Replace("^~~~~~~~~~~~~~~^", " ");
                        
        }
             
        internal void SetDependancies(List<string> tables, List<string> views)
        {
            _tableDependancies = QueryExpress.GetDependancies(_createTriggerSQL, tables);
        }


        internal void Export(List<string> tablesToExport, List<string> exportedViews,
            SQLiteBackup manager)
        {

            if (_isProcessed || manager.stopProcess || _createTriggerSQL.Trim().Length == 0) return;

            foreach (var dependantTable in _tableDependancies)
                {
                    // skip trigers that are dependant on skiped tables
                    if (!tablesToExport.Contains(dependantTable))
                        return;
                }
            foreach (var dependantView in _viewsDependancies)
                {
                // skip trigers that are dependant on skiped views
                if (!exportedViews.Contains(dependantView))
                    return;
            }
            
            manager.Export_WriteLine(string.Format("DROP TRIGGER IF EXISTS `{0}`;", _name));
            manager.Export_WriteLine(_createTriggerSQL);
            manager.textWriter.WriteLine();

            _isProcessed = true;

        }

    }
}
