using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace System.Data.SQLite
{
    public class SQLiteColumn
    {
        public enum DataWrapper
        {
            None,
            Sql
        }

        string _name = "";
        Type _dataType = typeof(string);
        string _sqliteDataType = "";
        bool _allowNull = true;
        string _defaultValue = "";
        bool _isPrimaryKey = false;
        
        public string Name { get { return _name; } }
        public Type DataType { get { return _dataType; } }
        public string SQLiteDataType { get { return _sqliteDataType; } }
        public bool AllowNull { get { return _allowNull; } }
        public string DefaultValue { get { return _defaultValue; } }
        public bool IsPrimaryKey { get { return _isPrimaryKey; } }

        public SQLiteColumn(string name, Type type, string sqliteDataType,
            bool allowNull, string defaultValue, bool isPrimaryKey)
        {
            _name = name;
            _dataType = type;
            _sqliteDataType = sqliteDataType.ToLower();
            _allowNull = allowNull;
            _defaultValue = defaultValue;
            _isPrimaryKey = isPrimaryKey;

        }
    }
}