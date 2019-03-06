using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;

namespace System.Data.SQLite
{
    public class QueryExpress
    {
        static NumberFormatInfo _numberFormatInfo = new NumberFormatInfo()
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty
        };

        static DateTimeFormatInfo _dateFormatInfo = new DateTimeFormatInfo()
        {
            DateSeparator = "-",
            TimeSeparator = ":"
        };

        static string[] _tableNameWildcarts = { "({0} ", "){0} ", ",{0} ", " {0}(", " {0})", " {0},", " {0}.",
            " {0} ", "`{0}`", ",{0},", "({0})", ",{0}.", ",{0})", "({0},", "){0}," };


        public static NumberFormatInfo SQLiteNumberFormat { get { return _numberFormatInfo; } }

        public static DateTimeFormatInfo SQLiteDateTimeFormat { get { return _dateFormatInfo; } }

        public static DataTable GetTable(SQLiteCommand cmd, string sql)
        {
            DataTable dt = new DataTable();
            cmd.CommandText = sql;
            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            return dt;
        }

        public static string ExecuteScalarStr(SQLiteCommand cmd, string sql)
        {
            cmd.CommandText = sql;
            object ob = cmd.ExecuteScalar();
            if (ob is byte[])
                return Encoding.UTF8.GetString((byte[])ob);
            else
                return ob + "";
        }

        public static string ExecuteScalarStr(SQLiteCommand cmd, string sql, int columnIndex)
        {
            DataTable dt = GetTable(cmd, sql);

            if (dt.Rows[0][columnIndex] is byte[])
                return Encoding.UTF8.GetString((byte[])dt.Rows[0][columnIndex]);
            else
                return dt.Rows[0][columnIndex] + "";
        }

        public static string ExecuteScalarStr(SQLiteCommand cmd, string sql, string columnName)
        {
            DataTable dt = GetTable(cmd, sql);

            if (dt.Rows[0][columnName] is byte[])
                return Encoding.UTF8.GetString((byte[])dt.Rows[0][columnName]);
            else
                return dt.Rows[0][columnName] + "";
        }

        public static long ExecuteScalarLong(SQLiteCommand cmd, string sql)
        {
            long l = 0;
            cmd.CommandText = sql;
            long.TryParse(cmd.ExecuteScalar() + "", out l);
            return l;
        }

        public static string EscapeStringSequence(string data)
        {
            var builder = new StringBuilder();
            foreach (var ch in data)
            {
                switch (ch)
                {
                    case '\'': // Single quotation mark
                        builder.AppendFormat("''");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        public static string ConvertToSqlFormat(object ob, bool wrapStringWithSingleQuote, bool escapeStringSequence, SQLiteColumn col)
        {
            StringBuilder sb = new StringBuilder();

            if (ob == null || ob is System.DBNull)
            {
                sb.AppendFormat("NULL");
            }
            else if (ob is System.String)
            {
                string str = (string)ob;

                if (escapeStringSequence)
                    str = QueryExpress.EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.Boolean)
            {
                sb.AppendFormat(Convert.ToInt32(ob).ToString());
            }
            else if (ob is System.Byte[])
            {
                if (((byte[])ob).Length == 0)
                {
                    return "NULL";
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("X'");
                    sb.AppendFormat(CryptoExpress.ConvertByteArrayToHexString((byte[])ob));
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
            }
            else if (ob is short)
            {
                sb.AppendFormat(((short)ob).ToString(_numberFormatInfo));
            }
            else if (ob is int)
            {
                sb.AppendFormat(((int)ob).ToString(_numberFormatInfo));
            }
            else if (ob is long)
            {
                sb.AppendFormat(((long)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ushort)
            {
                sb.AppendFormat(((ushort)ob).ToString(_numberFormatInfo));
            }
            else if (ob is uint)
            {
                sb.AppendFormat(((uint)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ulong)
            {
                sb.AppendFormat(((ulong)ob).ToString(_numberFormatInfo));
            }
            else if (ob is double)
            {
                sb.AppendFormat(((double)ob).ToString(_numberFormatInfo));
            }
            else if (ob is decimal)
            {
                sb.AppendFormat(((decimal)ob).ToString(_numberFormatInfo));
            }
            else if (ob is float)
            {
                sb.AppendFormat(((float)ob).ToString(_numberFormatInfo));
            }
            else if (ob is byte)
            {
                sb.AppendFormat(((byte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is sbyte)
            {
                sb.AppendFormat(((sbyte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)ob;

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(ts.Hours.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Minutes.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Seconds.ToString().PadLeft(2, '0'));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(((DateTime)ob).ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.Guid)
            {
                if (col.SQLiteDataType == "blob")
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("X'");

                    sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid)ob).ToByteArray()));
                    
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);
                    
                }

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType().ToString() + ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }

        public static string ConvertToSqlFormat(SQLiteDataReader rdr, int colIndex, bool wrapStringWithSingleQuote, bool escapeStringSequence, SQLiteColumn col)
        {
            object ob = rdr[colIndex];

            StringBuilder sb = new StringBuilder();

            if (ob == null || ob is System.DBNull)
            {
                sb.AppendFormat("NULL");
            }
            else if (ob is System.String)
            {
                string str = (string)ob;

                if (escapeStringSequence)
                    str = QueryExpress.EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.Boolean)
            {
                sb.AppendFormat(Convert.ToInt32(ob).ToString());
            }
            else if (ob is System.Byte[])
            {
                if (((byte[])ob).Length == 0)
                {
                    return "NULL";
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("X'");
                    sb.AppendFormat(CryptoExpress.ConvertByteArrayToHexString((byte[])ob));
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
            }
            else if (ob is short)
            {
                sb.AppendFormat(((short)ob).ToString(_numberFormatInfo));
            }
            else if (ob is int)
            {
                sb.AppendFormat(((int)ob).ToString(_numberFormatInfo));
            }
            else if (ob is long)
            {
                sb.AppendFormat(((long)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ushort)
            {
                sb.AppendFormat(((ushort)ob).ToString(_numberFormatInfo));
            }
            else if (ob is uint)
            {
                sb.AppendFormat(((uint)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ulong)
            {
                sb.AppendFormat(((ulong)ob).ToString(_numberFormatInfo));
            }
            else if (ob is double)
            {
                sb.AppendFormat(((double)ob).ToString(_numberFormatInfo));
            }
            else if (ob is decimal)
            {
                sb.AppendFormat(((decimal)ob).ToString(_numberFormatInfo));
            }
            else if (ob is float)
            {
                sb.AppendFormat(((float)ob).ToString(_numberFormatInfo));
            }
            else if (ob is byte)
            {
                sb.AppendFormat(((byte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is sbyte)
            {
                sb.AppendFormat(((sbyte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)ob;

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(ts.Hours.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Minutes.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Seconds.ToString().PadLeft(2, '0'));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(((DateTime)ob).ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));
                
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.Guid)
            {
                if (col.SQLiteDataType == "blob")
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("X'");

                    sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid)ob).ToByteArray()));
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);

                }
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType().ToString() + ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }


        internal static List<string> GetDependancies(string sql, List<string> tableNames)
        {

            var result = new List<string>();
            foreach (var tableName in tableNames)
            {
                if (HasDependencyOn(sql, tableName)) result.Add(tableName.Trim().ToLower());
            }

            return result;

        }

        static bool HasDependencyOn(string sql, string tableName)
        {
            var lowerCasedSql = sql.ToLower();
            var lowerCasedTableName = tableName.ToLower();
            foreach (var wildcart in _tableNameWildcarts)
            {
                if (lowerCasedSql.Contains(string.Format(wildcart, lowerCasedTableName))) return true;
            }
            return false;
        }

    }
}