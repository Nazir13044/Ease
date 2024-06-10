using Collecto.CoreAPI.TransactionManagement.Molels;
using System.Data;
using System.Globalization;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    public sealed class SQLParser
    {
        private static SQLSyntax _sqlSyntax;

        static SQLParser()
        {
            _sqlSyntax = SQLSyntax.SQL;
        }

        private static string GetDateLiteral(DateTime dt)
        {
            return _sqlSyntax switch
            {
                SQLSyntax.Access => "#" + dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + "#",
                SQLSyntax.Oracle => "TO_DATE('" + dt.ToString("dd MM yyyy", CultureInfo.InvariantCulture) + "', 'DD MM YYYY')",
                SQLSyntax.Informix => string.Format("DATETIME ({0}) YEAR TO DAY", dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                SQLSyntax.MySql => "'" + dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'",
                SQLSyntax.SQL => "'" + dt.Date.ToString("s", CultureInfo.InvariantCulture) + "'",
                _ => "'" + dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + "'",
            };
        }

        private static string GetDateTimeLiteral(DateTime dt)
        {
            return _sqlSyntax switch
            {
                SQLSyntax.Access => "#" + dt.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + "#",
                SQLSyntax.Oracle => "TO_DATE('" + dt.ToString("dd MM yyyy HH mm ss", CultureInfo.InvariantCulture) + "', 'DD MM YYYY HH24 MI SS')",
                SQLSyntax.Informix => string.Format("DATETIME ({0}) YEAR TO SECOND", dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                SQLSyntax.MySql => "'" + dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "'",
                SQLSyntax.SQL => "'" + dt.ToString("s", CultureInfo.InvariantCulture) + "'",
                _ => "'" + dt.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + "'",
            };
        }

        private static string GetTimeLiteral(DateTime dt)
        {
            return _sqlSyntax switch
            {
                SQLSyntax.Access => "#" + dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "#",
                SQLSyntax.Oracle => "TO_DATE('" + dt.ToString("HH mm ss", CultureInfo.InvariantCulture) + "', 'HH24 MI SS')",
                SQLSyntax.Informix => string.Format("DATETIME ({0}) HOUR TO SECOND", dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                SQLSyntax.MySql => "'" + dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "'",
                SQLSyntax.SQL => "'" + dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "'",
                _ => "'" + dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "'",
            };
        }

        public static string MakeSQL(SQLSyntax sqlSyntax, string sql, params object[] args)
        {
            _sqlSyntax = sqlSyntax;
            return MakeSQL(sql, args);
        }

        public static string MakeSQL(string sql, params object[] args)
        {
            if (args.Length == 0)
            {
                return sql;
            }

            int num = -1;
            string text = string.Empty;
            string[] array = new string[args.Length];
            int num2;
            for (num2 = sql.IndexOf("%"); num2 != -1; num2 = sql.IndexOf("%"))
            {
                text += sql.Substring(0, num2);
                if (num2 == sql.Length - 1)
                {
                    throw new InvalidExpressionException("Invalid place holder character.");
                }

                string text2 = sql.Substring(num2 + 1, 1);
                string text3 = sql;
                int num3 = num2 + 2;
                sql = text3.Substring(num3, text3.Length - num3);
                if (text2.IndexOfAny(new char[2] { '%', '{' }) != -1)
                {
                    if (!(text2 == "%"))
                    {
                        if (text2 == "{")
                        {
                            int num4 = sql.IndexOf("}");
                            if (num4 < 1)
                            {
                                throw new InvalidExpressionException("Invalid rrdinal variable.");
                            }

                            int num5 = Convert.ToInt32(sql.Substring(0, num4));
                            if (num5 < 0 || num5 > num)
                            {
                                throw new IndexOutOfRangeException("Invalid rrdinal variable.");
                            }

                            text += array[num5];
                            text3 = sql;
                            num3 = num4 + 1;
                            sql = text3.Substring(num3, text3.Length - num3);
                        }
                    }
                    else
                    {
                        text += "%";
                    }
                }
                else
                {
                    if (text2.IndexOfAny(new char[8] { 's', 'n', 'd', 't', 'D', 'b', 'q', 'u' }) == -1)
                    {
                        throw new InvalidExpressionException("Invalid place holder character.");
                    }

                    if (++num > args.Length - 1)
                    {
                        throw new ArgumentException("Too few arguments passed.");
                    }

                    if (args[num] == null)
                    {
                        array[num] = "NULL";
                    }
                    else
                    {
                        try
                        {
                            if (text2 != null)
                            {
                                num3 = text2.Length;
                                if (num3 == 1)
                                {
                                    switch (text2[0])
                                    {
                                        case 's':
                                            {
                                                string text6 = Convert.ToString(args[num], CultureInfo.InvariantCulture);
                                                array[num] = "'" + text6.Replace("'", "''") + "'";
                                                break;
                                            }
                                        case 'u':
                                            {
                                                string text5 = Convert.ToString(args[num], CultureInfo.InvariantCulture);
                                                array[num] = "N'" + text5.Replace("'", "''") + "'";
                                                break;
                                            }
                                        case 'n':
                                            array[num] = Convert.ToDecimal(args[num], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            break;
                                        case 'd':
                                            {
                                                DateTime dt2 = Convert.ToDateTime(args[num], CultureInfo.InvariantCulture);
                                                array[num] = GetDateLiteral(dt2);
                                                break;
                                            }
                                        case 't':
                                            {
                                                DateTime dt = Convert.ToDateTime(args[num], CultureInfo.InvariantCulture);
                                                array[num] = GetTimeLiteral(dt);
                                                break;
                                            }
                                        case 'D':
                                            {
                                                DateTime dt3 = Convert.ToDateTime(args[num], CultureInfo.InvariantCulture);
                                                array[num] = GetDateTimeLiteral(dt3);
                                                break;
                                            }
                                        case 'b':
                                            {
                                                bool flag = Convert.ToBoolean(args[num], CultureInfo.InvariantCulture);
                                                if (_sqlSyntax == SQLSyntax.Access)
                                                {
                                                    array[num] = flag.ToString(CultureInfo.InvariantCulture);
                                                }
                                                else
                                                {
                                                    array[num] = (flag ? "1" : "0");
                                                }

                                                break;
                                            }
                                        case 'q':
                                            {
                                                string text4 = Convert.ToString(args[num], CultureInfo.InvariantCulture);
                                                array[num] = text4;
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            throw new ArgumentException($"Invalid argument no: {num}");
                        }
                    }

                    text += array[num];
                }
            }

            text += sql;
            int num6 = text.ToUpper().IndexOf("WHERE");
            num2 = text.IndexOf("==");
            while (num2 != -1)
            {
                if (num6 < 0 || num6 > num2)
                {
                    num2 = -1;
                    continue;
                }

                string text7 = text;
                int num3 = num2 + 2;
                string text3 = text7.Substring(num3, text7.Length - num3).Trim();
                if (text3.Substring(0, text3.Length).ToUpper().StartsWith("NULL"))
                {
                    string text8 = text.Substring(0, num2);
                    text3 = text;
                    num3 = num2 + 2;
                    text = text8 + " IS " + text3.Substring(num3, text3.Length - num3);
                }
                else
                {
                    string text9 = text.Substring(0, num2);
                    text3 = text;
                    num3 = num2 + 2;
                    text = text9 + "=" + text3.Substring(num3, text3.Length - num3);
                }

                num2 = text.IndexOf("==", num2 + 2);
            }

            num2 = text.IndexOf("<>");
            while (num2 != -1)
            {
                if (num6 < 0 || num6 > num2)
                {
                    num2 = -1;
                    continue;
                }

                string text7 = text;
                int num3 = num2 + 2;
                string text3 = text7.Substring(num3, text7.Length - num3).Trim();
                if (text3.Substring(1, text3.Length - 1).ToUpper().StartsWith("NULL"))
                {
                    string text10 = text.Substring(0, num2);
                    text3 = text;
                    num3 = num2 + 2;
                    text = text10 + " IS NOT " + text3.Substring(num3, text3.Length - num3);
                }

                num2 = text.IndexOf("<>", num2 + 2);
            }

            return text;
        }

        public static string TagSQL(string commandText)
        {
            return string.Format("{0} {1} ", commandText.Trim(), (commandText.Trim().Length <= 0) ? "WHERE" : "AND");
        }
    }
}
