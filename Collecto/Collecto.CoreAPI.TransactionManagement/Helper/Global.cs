using Collecto.CoreAPI.TransactionManagement.Molels;
using Collecto.CoreAPI.TransactionManagement.Utility;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Collecto.CoreAPI.TransactionManagement.Helper
{
    public sealed class Global
    {
        public static object lobal;

        public class StringFunctions
        {
            public static bool IsEmptyOrNull(object o)
            {
                return IsEmptyOrNull((o == null) ? string.Empty : o.ToString());
            }

            public static bool IsEmptyOrNull(string s)
            {
                if (s == null)
                {
                    s = string.Empty;
                }

                return string.IsNullOrEmpty(s);
            }

            public static bool IsEmpty(object o)
            {
                return IsEmpty((o == null) ? string.Empty : o.ToString());
            }

            public static bool IsEmpty(string s)
            {
                if (s == null)
                {
                    s = string.Empty;
                }

                if (!(s == string.Empty))
                {
                    return s.Trim().Length <= 0;
                }

                return true;
            }

            public static bool IsNotEmpty(string s)
            {
                return !IsEmpty(s);
            }

            public static bool IsNotEmpty(object o)
            {
                return !IsEmpty(o);
            }

            public static string FormatString(string format, params object[] args)
            {
                return string.Format(format, args);
            }

            public static string StringReverse(string s)
            {
                string text = string.Empty;
                for (int num = s.Length - 1; num >= 0; num--)
                {
                    text = $"{text}{s[num]}";
                }

                return text;
            }

            public static string MakeItSentence(string inputString)
            {
                for (int i = 0; i < inputString.Length; i++)
                {
                    if (char.IsUpper(Convert.ToChar(inputString.Substring(i, 1))) && i != 0 && inputString.Substring(i - 1, 1) != " ")
                    {
                        inputString = inputString.Insert(i, " ");
                    }
                }

                return inputString;
            }

            public static string Left(string inputString, int length)
            {
                if (length < inputString.Length && length > 0)
                {
                    return inputString.Substring(0, length);
                }

                return inputString;
            }

            public static string Right(string inputString, int length)
            {
                if (length < inputString.Length && length > 0)
                {
                    return inputString.Substring(inputString.Length - length, length);
                }

                return inputString;
            }

            public static string Mid(string inputString, int start, int length)
            {
                if (start + length < inputString.Length)
                {
                    return inputString.Substring(start, length);
                }

                if (start < inputString.Length)
                {
                    return inputString.Substring(start, inputString.Length - start);
                }

                return string.Empty;
            }

            public static string Mid(string inputString, int start)
            {
                if (start < inputString.Length)
                {
                    return inputString.Substring(start, inputString.Length - start);
                }

                return string.Empty;
            }

            public static void Mid(ref string inputString, int start, int length, string value)
            {
                if (start + length > inputString.Length)
                {
                    throw new Exception("Inputstring has not enough length");
                }

                string arg = inputString.Substring(0, start);
                string text = inputString;
                int num = start + length;
                string arg2 = text.Substring(num, text.Length - num);
                value = ((value.Length < length) ? value.PadRight(length, ' ') : value);
                value = ((value.Length > length) ? Left(value, length) : value);
                inputString = $"{arg}{value}{arg2}";
            }

            public static bool IsStrongPassword(string password)
            {
                return IsStrongPassword(password, 4, 30);
            }

            public static bool IsStrongPassword(string password, int minLen, int maxLen)
            {
                if (minLen > 0 && minLen > password.Length)
                {
                    throw new Exception($"Minimum length of Password: {minLen}");
                }

                if (maxLen > 0 && password.Length > maxLen)
                {
                    throw new Exception($"Maximum length of Password: {maxLen}");
                }

                bool result = false;
                HashSet<char> hashSet = new HashSet<char> { '!', '?', '#', '%', '*' };
                if (password.Any(char.IsLower) && password.Any(char.IsUpper) && password.Any(char.IsDigit) && password.Any(hashSet.Contains))
                {
                    result = true;
                }

                return result;
            }

            public static bool IsEmailAddress(string emailAddress)
            {
                string pattern = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$";
                return Regex.IsMatch(emailAddress, pattern);
            }
        }

        public class DateFunctions
        {
            public class DateDifference
            {
                private readonly int _year;

                private readonly int _month;

                private readonly int _day;

                private readonly DateTime _fromDate;

                private readonly DateTime _toDate;

                private readonly int[] _monthDay =
                [
                31, -1, 31, 30, 31, 30, 31, 31, 30, 31,
                30, 31
                ];

                public int Years => _year;

                public int Months => _month;

                public int Days => _day;

                public DateDifference(DateTime date1, DateTime date2)
                {
                    _toDate = ((date1 > date2) ? date1 : date2);
                    _fromDate = ((date1 > date2) ? date2 : date1);
                    int num = 0;
                    if (_fromDate.Day > _toDate.Day)
                    {
                        num = _monthDay[_fromDate.Month - 1];
                    }

                    if (num == -1)
                    {
                        num = ((!DateTime.IsLeapYear(_fromDate.Year)) ? 28 : 29);
                    }

                    if (num != 0)
                    {
                        _day = _toDate.Day + num - _fromDate.Day;
                        num = 1;
                    }
                    else
                    {
                        _day = _toDate.Day - _fromDate.Day;
                    }

                    if (_fromDate.Month + num > _toDate.Month)
                    {
                        _month = _toDate.Month + 12 - (_fromDate.Month + num);
                        num = 1;
                    }
                    else
                    {
                        _month = _toDate.Month - (_fromDate.Month + num);
                        num = 0;
                    }

                    _year = _toDate.Year - (_fromDate.Year + num);
                }

                public override string ToString()
                {
                    string text = string.Empty;
                    if (_year > 0)
                    {
                        text = string.Format("{0} Year{1} ", _year, (_year > 1) ? "s" : "");
                    }

                    if (_month > 0)
                    {
                        text += string.Format("{0} {1}onth{2} ", _month, (text.Length > 0) ? "m" : "M", (_month > 1) ? "s" : "");
                    }

                    if (_day > 0)
                    {
                        text += string.Format("{0} {1}ay{2} ", _day, (text.Length > 0) ? "d" : "D", (_day > 1) ? "s" : "");
                    }

                    return text.TrimEnd();
                }

                public string ToString(bool abbreviated)
                {
                    string text = string.Empty;
                    if (!abbreviated)
                    {
                        text = ToString();
                    }
                    else
                    {
                        if (_year > 0)
                        {
                            text = $"{_year} Y";
                        }

                        if (_month > 0)
                        {
                            text += string.Format("{0} {1} ", _month, (text.Length > 0) ? "m" : "M");
                        }

                        if (_day > 0)
                        {
                            text += string.Format("{0} {1} ", _day, (text.Length > 0) ? "d" : "D");
                        }
                    }

                    return text.TrimEnd();
                }
            }

            public static bool IsDateTime(string s, FormatOptions f)
            {
                if (string.IsNullOrEmpty(s))
                {
                    throw new Exception("Invalid use of empty string.");
                }

                string[] array = s.Split('-', '/', '.', ' ');
                if (array == null || array.Length < 3)
                {
                    throw new Exception("Invalid use of date format.");
                }

                bool flag = true;
                if (f == FormatOptions.MMddyyyy)
                {
                    if (!NumericFunctions.IsNumeric(array[1].Trim()))
                    {
                        return false;
                    }

                    int num = Convert.ToInt32(array[1].Trim());
                    if (!NumericFunctions.IsNumeric(array[0].Trim()))
                    {
                        return false;
                    }

                    int num2 = Convert.ToInt32(array[0].Trim());
                    if (!NumericFunctions.IsNumeric(array[2].Trim()))
                    {
                        return false;
                    }

                    int num3 = Convert.ToInt32(array[2].Trim());
                    if (num3 < 100 && array[2].Trim().Length < 4)
                    {
                        num3 = 2000 + num3;
                    }

                    if (num2 < 1 || num2 > 12)
                    {
                        flag = false;
                    }

                    if (flag && (num < 1 || num > DateTime.DaysInMonth(num3, num2)))
                    {
                        flag = false;
                    }
                }

                if (f == FormatOptions.ddMMyyyy)
                {
                    if (!NumericFunctions.IsNumeric(array[0].Trim()))
                    {
                        return false;
                    }

                    int num = Convert.ToInt32(array[0].Trim());
                    if (!NumericFunctions.IsNumeric(array[1].Trim()))
                    {
                        return false;
                    }

                    int num2 = Convert.ToInt32(array[1].Trim());
                    if (!NumericFunctions.IsNumeric(array[2].Trim()))
                    {
                        return false;
                    }

                    int num3 = Convert.ToInt32(array[2].Trim());
                    if (num3 < 100 && array[2].Trim().Length < 4)
                    {
                        num3 = 2000 + num3;
                    }

                    if (num2 < 1 || num2 > 12)
                    {
                        flag = false;
                    }

                    if (flag && (num < 1 || num > DateTime.DaysInMonth(num3, num2)))
                    {
                        flag = false;
                    }
                }

                if (f == FormatOptions.ddMMMyyyy)
                {
                    if (!NumericFunctions.IsNumeric(array[0].Trim()))
                    {
                        return false;
                    }

                    int num = Convert.ToInt32(array[0].Trim());
                    if (!NumericFunctions.IsNumeric(array[2].Trim()))
                    {
                        return false;
                    }

                    int num3 = Convert.ToInt32(array[2].Trim());
                    if (num3 < 100 && array[2].Trim().Length < 4)
                    {
                        num3 = 2000 + num3;
                    }

                    int num2 = MonthIndex(array[1]);
                    if (num2 < 1 || num2 > 12)
                    {
                        flag = false;
                    }

                    if (flag && (num < 1 || num > DateTime.DaysInMonth(num3, num2)))
                    {
                        flag = false;
                    }
                }

                return flag;
            }

            public static bool IsDateTime(object o, FormatOptions f)
            {
                return IsDateTime((o == null) ? string.Empty : o.ToString(), f);
            }

            public static DateTime GetDate(string s, FormatOptions f)
            {
                if (StringFunctions.IsEmptyOrNull(s))
                {
                    throw new Exception("Invalid use of empty string.");
                }

                string[] array = s.Split('-', '/', '.', ' ');
                if (array == null || array.Length < 3)
                {
                    throw new Exception("Invalid use of date format.");
                }

                switch (f)
                {
                    case FormatOptions.MMddyyyy:
                        {
                            int num = Convert.ToInt32(array[1].Trim());
                            int num3 = Convert.ToInt32(array[0].Trim());
                            int num2 = Convert.ToInt32(array[2].Trim());
                            if (num2 < 100 && array[2].Trim().Length < 4)
                            {
                                num2 = 2000 + num2;
                            }

                            if (num3 < 1 || num3 > 12)
                            {
                                throw new Exception("Invalid date format for month overflow.");
                            }

                            if (num < 1 || num > DateTime.DaysInMonth(num2, num3))
                            {
                                throw new Exception("Invalid date format for day overflow.");
                            }

                            return new DateTime(num2, num3, num);
                        }
                    case FormatOptions.ddMMyyyy:
                        {
                            int num = Convert.ToInt32(array[0].Trim());
                            int num3 = Convert.ToInt32(array[1].Trim());
                            int num2 = Convert.ToInt32(array[2].Trim());
                            if (num2 < 100 && array[2].Trim().Length < 4)
                            {
                                num2 = 2000 + num2;
                            }

                            if (num3 < 1 || num3 > 12)
                            {
                                throw new Exception("Invalid date format for month overflow.");
                            }

                            if (num < 1 || num > DateTime.DaysInMonth(num2, num3))
                            {
                                throw new Exception("Invalid date format for day overflow.");
                            }

                            return new DateTime(num2, num3, num);
                        }
                    case FormatOptions.ddMMMyyyy:
                        {
                            int num = Convert.ToInt32(array[0].Trim());
                            int num2 = Convert.ToInt32(array[2].Trim());
                            if (num2 < 100 && array[2].Trim().Length < 4)
                            {
                                num2 = 2000 + num2;
                            }

                            int num3 = MonthIndex(array[1]);
                            if (num3 < 1 || num3 > 12)
                            {
                                throw new Exception("Invalid date format for month overflow.");
                            }

                            if (num < 1 || num > DateTime.DaysInMonth(num2, num3))
                            {
                                throw new Exception("Invalid date format for day overflow.");
                            }

                            return new DateTime(num2, num3, num);
                        }
                    default:
                        return new DateTime(1, 1, 1);
                }
            }

            public static DateTime GetDate(object o, FormatOptions f)
            {
                if (o == null)
                {
                    throw new Exception("Invalid use of null reference.");
                }

                return GetDate(o.ToString(), f);
            }

            public static DateTime FirstDateOfMonth(DateTime forDate)
            {
                return new DateTime(forDate.Year, forDate.Month, 1);
            }

            public static DateTime FirstDateOfMonth(int year, int month)
            {
                return new DateTime(year, month, 1);
            }

            public static DateTime FirstDateOfYear(DateTime forDate)
            {
                return new DateTime(forDate.Year, 1, 1);
            }

            public static DateTime FirstDateOfYear(int year)
            {
                return new DateTime(year, 1, 1);
            }

            public static DateTime LastDateOfYear(DateTime forDate)
            {
                return new DateTime(forDate.Year, 12, 31);
            }

            public static DateTime LastDateOfYear(int year)
            {
                return new DateTime(year, 12, 31);
            }

            public static DateTime LastDateOfMonth(DateTime forDate)
            {
                return new DateTime(forDate.Year, forDate.Month, 1).AddMonths(1).AddDays(-1.0);
            }

            public static DateTime LastDateOfMonth(int year, int month)
            {
                return new DateTime(year, month, 1).AddMonths(1).AddDays(-1.0);
            }

            public static bool IsFirstDayOfMonth(DateTime onDate)
            {
                return onDate.Day == 1;
            }

            public static bool IsFirstDayOfYear(DateTime onDate)
            {
                if (onDate.Month == 1)
                {
                    return onDate.Day == 1;
                }

                return false;
            }

            public static bool IsLastDayOfMonth(DateTime onDate)
            {
                return DateTime.DaysInMonth(onDate.Year, onDate.Month) <= onDate.Day;
            }

            public static bool IsLastDayOfYear(DateTime onDate)
            {
                if (onDate.Month == 12)
                {
                    return onDate.Day == 31;
                }

                return false;
            }

            public static int DateDiff(string interval, DateTime startDate, DateTime endDate)
            {
                DateDifference dateDifference = new DateDifference(startDate, endDate);
                int num = 0;
                DateTime dateTime;
                DateTime dateTime2;
                if (startDate > endDate)
                {
                    dateTime = startDate;
                    dateTime2 = endDate;
                }
                else
                {
                    dateTime2 = startDate;
                    dateTime = endDate;
                }

                switch (interval)
                {
                    case "D":
                    case "d":
                        {
                            for (int i = dateTime2.Year + 1; i <= dateTime.Year; i++)
                            {
                                num += new DateTime(i, 12, 31).DayOfYear;
                            }

                            num += dateTime.DayOfYear - dateTime2.DayOfYear;
                            break;
                        }
                    case "M":
                    case "m":
                        num = dateDifference.Years * 12;
                        num += dateDifference.Months;
                        break;
                    case "Y":
                    case "y":
                        num = dateDifference.Years;
                        break;
                }

                if (startDate > endDate)
                {
                    num = -num;
                }

                return num;
            }

            public static string DateDiff(DateTime date1, DateTime date2)
            {
                return new DateDifference(date1, date2).ToString();
            }

            public static int[] DateDiffYMD(DateTime date1, DateTime date2)
            {
                DateDifference dateDifference = new DateDifference(date1, date2);
                return new int[3] { dateDifference.Years, dateDifference.Months, dateDifference.Days };
            }

            public static DateTime DateAdd(string interval, int number, DateTime date)
            {
                DateTime result = date;
                switch (interval)
                {
                    case "D":
                    case "d":
                        result = result.AddDays(number);
                        break;
                    case "M":
                    case "m":
                        result = result.AddMonths(number);
                        break;
                    case "Y":
                    case "y":
                        result = result.AddYears(number);
                        break;
                }

                return result;
            }

            private static int MonthIndex(string monthName)
            {
                int num = 0;
                DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
                if (monthName.Trim().Length <= 2 && NumericFunctions.IsNumeric(monthName))
                {
                    return Convert.ToInt32(monthName.Trim());
                }

                string[] array = ((monthName.Trim().Length != 3) ? dateTimeFormatInfo.MonthNames : dateTimeFormatInfo.AbbreviatedMonthNames);
                string[] array2 = array;
                foreach (string obj in array2)
                {
                    num++;
                    if (obj.ToLower() == monthName.ToLower())
                    {
                        return num;
                    }
                }

                return -1;
            }

            public static List<DateTime> DatesOfDay(DayOfWeek day, DateTime date1, DateTime date2)
            {
                return DatesOfDay(day, date1, date2, null);
            }

            public static List<DateTime> DatesOfDay(DayOfWeek day, DateTime date1, DateTime date2, List<DateTime> holidays)
            {
                List<DateTime> list = new List<DateTime>();
                DateTime dateTime = ((date1 < date2) ? date1 : date2);
                date2 = ((date1 > date2) ? date1 : date2);
                while (dateTime.DayOfWeek != day)
                {
                    dateTime = dateTime.AddDays(1.0);
                }

                while (dateTime <= date2)
                {
                    if (holidays == null || !holidays.Contains(dateTime))
                    {
                        list.Add(dateTime);
                    }

                    dateTime = dateTime.AddDays(7.0);
                }

                return list;
            }
        }

        public class NumericFunctions
        {
            private static string HundredWords(int value)
            {
                string[] array = new string[10] { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
                string[] array2 = new string[10] { "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] array3 = new string[10] { "", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
                string inputString = StringFunctions.Right(value.ToString("000", CultureInfo.InvariantCulture), 3);
                string text = ((!(StringFunctions.Left(inputString, 1) != "0")) ? "" : (array.GetValue(Convert.ToInt32(StringFunctions.Left(inputString, 1)))?.ToString() + " hundred"));
                inputString = StringFunctions.Right(inputString, 2);
                string text2;
                string text3;
                if (StringFunctions.Left(inputString, 1) == "1")
                {
                    text2 = Convert.ToString(array2.GetValue(Convert.ToInt32(StringFunctions.Right(inputString, 1))));
                    text3 = "";
                }
                else
                {
                    text2 = Convert.ToString(array3.GetValue(Convert.ToInt32(StringFunctions.Left(inputString, 1))));
                    text3 = Convert.ToString(array.GetValue(Convert.ToInt32(StringFunctions.Right(inputString, 1))));
                }

                string text4 = text;
                if (text4 != "" && text2 != "")
                {
                    text4 += " ";
                }

                text4 += text2;
                if (text4 != "" && text3 != "")
                {
                    text4 += " ";
                }

                return text4 + text3;
            }

            public static string InWords(decimal inputValue, string replaceDecimalBy)
            {
                return InWords(Convert.ToDouble(inputValue), replaceDecimalBy);
            }

            public static string InWords(float inputValue, string replaceDecimalBy)
            {
                return InWords(Convert.ToDouble(inputValue), replaceDecimalBy);
            }

            public static string InWords(double value, string replaceDecimalBy)
            {
                string text = "";
                string[] array = new string[3] { "crore", "thousand", "lac" };
                string arg = ((value < 0.0) ? "Minus" : string.Empty);
                value = Math.Abs(value);
                string text2 = value.ToString("0.00", CultureInfo.InvariantCulture);
                string text3 = StringFunctions.Right(text2, 2);
                if (Convert.ToInt32(text3) > 0)
                {
                    for (int i = 0; i < text3.Length; i++)
                    {
                        text += " ";
                        string text4 = HundredWords(Convert.ToInt32(text3[i].ToString()));
                        text4 = ((text4.Trim().Length <= 0) ? "zero" : text4);
                        text += text4;
                    }
                }

                text = text.Trim();
                if (text.Trim().Length > 0)
                {
                    string text5 = text.Substring(0, 1).ToUpper();
                    string text6 = text;
                    text = text5 + text6.Substring(1, text6.Length - 1).ToLower();
                    text = $"{replaceDecimalBy} {text}".Trim();
                }

                text2 = StringFunctions.Left(text2, text2.Length - 3);
                string text7 = HundredWords(Convert.ToInt32(StringFunctions.Right(text2, 3)));
                text2 = ((text2.Length <= 3) ? string.Empty : StringFunctions.Left(text2, text2.Length - 3));
                int num = 1;
                while (text2.Length > 0)
                {
                    int num2 = ((num % 3 == 0) ? 3 : 2);
                    string text8 = HundredWords(Convert.ToInt32(StringFunctions.Right(text2, num2)));
                    if (text8.Length > 0)
                    {
                        text8 += " ";
                        text8 += ((text2.Length > 10) ? (array.GetValue(num % 3)?.ToString() + " crore ") : array.GetValue(num % 3));
                    }

                    if (text7.Length > 0 && text8.Length > 0)
                    {
                        text8 += " ";
                    }

                    text7 = text8 + text7;
                    text2 = ((text2.Length <= num2) ? string.Empty : StringFunctions.Left(text2, text2.Length - num2));
                    num++;
                }

                if (text7.Length > 0)
                {
                    string text9 = text7.Substring(0, 1).ToUpper();
                    string text6 = text7;
                    text7 = text9 + text6.Substring(1, text6.Length - 1).ToLower();
                }
                else
                {
                    text7 = "Zero";
                }

                return $"{arg} {text7} {text}".Trim();
            }

            private static string Words(double value, string beforeDecimal, string afterDecimal)
            {
                string[] array = new string[3] { "crore", "thousand", "lac" };
                string arg = ((value < 0.0) ? "Minus" : string.Empty);
                value = Math.Abs(value);
                string text = value.ToString("0.00", CultureInfo.InvariantCulture);
                string text2 = HundredWords(Convert.ToInt32(StringFunctions.Right(text, 2)));
                if (text2.Trim().Length > 0)
                {
                    string text3 = text2.Substring(0, 1).ToUpper();
                    string text4 = text2;
                    text2 = text3 + text4.Substring(1, text4.Length - 1).ToLower();
                    text2 = $"{afterDecimal} {text2}";
                }

                text = StringFunctions.Left(text, text.Length - 3);
                string text5 = HundredWords(Convert.ToInt32(StringFunctions.Right(text, 3)));
                text = ((text.Length <= 3) ? string.Empty : StringFunctions.Left(text, text.Length - 3));
                int num = 1;
                while (text.Length > 0)
                {
                    int num2 = ((num % 3 == 0) ? 3 : 2);
                    string text6 = HundredWords(Convert.ToInt32(StringFunctions.Right(text, num2)));
                    if (text6.Length > 0)
                    {
                        text6 += " ";
                        text6 += ((text.Length > 10) ? (array.GetValue(num % 3)?.ToString() + " crore ") : array.GetValue(num % 3));
                    }

                    if (text5.Length > 0 && text6.Length > 0)
                    {
                        text6 += " ";
                    }

                    text5 = text6 + text5;
                    text = ((text.Length <= num2) ? string.Empty : StringFunctions.Left(text, text.Length - num2));
                    num++;
                }

                if (text5.Length > 0)
                {
                    string text7 = text5.Substring(0, 1).ToUpper();
                    string text4 = text5;
                    text5 = beforeDecimal + " " + text7 + text4.Substring(1, text4.Length - 1).ToLower();
                }

                string text8 = text5;
                if (text8.Trim().Length > 0 && text2.Trim().Length > 0)
                {
                    text8 += " and ";
                }

                text8 += text2;
                if (text8.Trim().Length <= 0)
                {
                    text8 = beforeDecimal + " Zero";
                }

                text8 = $"{arg} {text8} Only";
                return text8.Trim();
            }

            public static string AmountInWords(decimal inputValue, string beforeDecimal, string afterDecimal)
            {
                return Words(Convert.ToDouble(inputValue), beforeDecimal, afterDecimal);
            }

            public static string AmountInWords(float inputValue, string beforeDecimal, string afterDecimal)
            {
                return Words(Convert.ToDouble(inputValue), beforeDecimal, afterDecimal);
            }

            public static string AmountInWords(double inputValue, string beforeDecimal, string afterDecimal)
            {
                return Words(inputValue, beforeDecimal, afterDecimal);
            }

            public static string TakaWords(decimal inputValue)
            {
                return TakaWords(Convert.ToDouble(inputValue));
            }

            public static string TakaWords(float inputValue)
            {
                return TakaWords(Convert.ToDouble(inputValue));
            }

            public static string TakaWords(double inputValue)
            {
                return Words(inputValue, "Taka", "Paisa");
            }

            public static bool IsNumeric(char chrValue)
            {
                return char.IsDigit(chrValue);
            }

            public static bool IsNumeric(string s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return false;
                }

                s = s.Trim();
                if (s.StartsWith("+") || s.StartsWith("-"))
                {
                    string text = s;
                    s = text.Substring(1, text.Length - 1);
                }

                foreach (char c in s)
                {
                    if (!char.IsNumber(c) && c != '.')
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool IsNumeric(object o)
            {
                string s = ((o == null) ? string.Empty : Convert.ToString(o));
                if (StringFunctions.IsNotEmpty(s))
                {
                    return IsNumeric(s);
                }

                return false;
            }

            public static bool IsDecimal(string strValue)
            {
                if (!string.IsNullOrEmpty(strValue))
                {
                    return !DecimalRegex().IsMatch(strValue);
                }

                return false;
            }

            public static bool IsCurrency(string s)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    return !CurrencyRegex().IsMatch(s);
                }

                return false;
            }

            public static double RoundOff(decimal inputValue)
            {
                return Convert.ToDouble(Math.Round(inputValue));
            }

            public static double RoundOff(decimal inputValue, int digits)
            {
                return RoundOff(Convert.ToDouble(inputValue), digits);
            }

            public static double RoundOff(double inputValue, int digits)
            {
                if (digits < 0)
                {
                    double num = Math.Pow(10.0, Math.Abs(digits));
                    double num2 = inputValue % num / num;
                    num2 = ((!(num2 >= 0.5)) ? 0.0 : (1.0 * num));
                    return (inputValue - inputValue % num) / num * num + num2;
                }

                return Math.Round(inputValue, digits);
            }

            public static double RoundOff(double inputValue)
            {
                return Math.Round(inputValue);
            }

            public static double RoundOff(float inputValue, int digits)
            {
                return RoundOff(Convert.ToDouble(inputValue), digits);
            }

            public static double RoundOff(float inputValue)
            {
                return Math.Round(inputValue);
            }

            public static string TakaFormat(decimal inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                return TakaFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator, digits);
            }

            public static string TakaFormat(decimal inputValue, byte digits)
            {
                return TakaFormat(Convert.ToDouble(inputValue), ',', '.', digits);
            }

            public static string TakaFormat(decimal inputValue, char digitSeparator, char decimalSeparator)
            {
                return TakaFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator);
            }

            public static string TakaFormat(decimal inputValue)
            {
                return TakaFormat(Convert.ToDouble(inputValue), ',', '.');
            }

            public static string TakaFormat(float inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                return TakaFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator, digits);
            }

            public static string TakaFormat(float inputValue, byte digits)
            {
                return TakaFormat(Convert.ToDouble(inputValue), ',', '.', digits);
            }

            public static string TakaFormat(float inputValue, char digitSeparator, char decimalSeparator)
            {
                return TakaFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator);
            }

            public static string TakaFormat(float inputValue)
            {
                return TakaFormat(Convert.ToDouble(inputValue), ',', '.');
            }

            public static string TakaFormat(double inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                string text = "##,##,###";
                if (digits > 0)
                {
                    text = $"{text}.{new string('0', digits)}";
                }

                return CustomFormat(inputValue, digitSeparator, decimalSeparator, text);
            }

            public static string TakaFormat(double inputValue, byte digits)
            {
                return TakaFormat(inputValue, ',', '.', digits);
            }

            public static string TakaFormat(double inputValue, char digitSeparator, char decimalSeparator)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, "##,##,###.00");
            }

            public static string TakaFormat(double inputValue)
            {
                return TakaFormat(inputValue, ',', '.');
            }

            public static string MillionFormat(decimal inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, $"N{digits}");
            }

            public static string MillionFormat(decimal inputValue, byte digits)
            {
                return CustomFormat(inputValue, ',', '.', $"N{digits}");
            }

            public static string MillionFormat(decimal inputValue, char digitSeparator, char decimalSeparator)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, "N2");
            }

            public static string MillionFormat(decimal inputValue)
            {
                return CustomFormat(inputValue, ',', '.', "N2");
            }

            public static string MillionFormat(float inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, $"N{digits}");
            }

            public static string MillionFormat(float inputValue, byte digits)
            {
                return CustomFormat(inputValue, ',', '.', $"N{digits}");
            }

            public static string MillionFormat(float inputValue, char digitSeparator, char decimalSeparator)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, "N2");
            }

            public static string MillionFormat(float inputValue)
            {
                return CustomFormat(inputValue, ',', '.', "N2");
            }

            public static string MillionFormat(double inputValue, char digitSeparator, char decimalSeparator, byte digits)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, $"N{digits}");
            }

            public static string MillionFormat(double inputValue, byte digits)
            {
                return CustomFormat(inputValue, ',', '.', $"N{digits}");
            }

            public static string MillionFormat(double inputValue, char digitSeparator, char decimalSeparator)
            {
                return CustomFormat(inputValue, digitSeparator, decimalSeparator, "N2");
            }

            public static string MillionFormat(double inputValue)
            {
                return CustomFormat(inputValue, ',', '.', "N2");
            }

            public static string CustomFormat(decimal inputValue, string format)
            {
                return CustomFormat(Convert.ToDouble(inputValue), ',', '.', format);
            }

            public static string CustomFormat(decimal inputValue, char digitSeparator, char decimalSeparator, string format)
            {
                return CustomFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator, format);
            }

            public static string CustomFormat(float inputValue, string format)
            {
                return CustomFormat(Convert.ToDouble(inputValue), ',', '.', format);
            }

            public static string CustomFormat(float inputValue, char digitSeparator, char decimalSeparator, string format)
            {
                return CustomFormat(Convert.ToDouble(inputValue), digitSeparator, decimalSeparator, format);
            }

            public static string CustomFormat(double inputValue, string format)
            {
                return CustomFormat(inputValue, ',', '.', format);
            }

            public static string CustomFormat(double inputValue, char digitSeparator, char decimalSeparator, string format)
            {
                string arg = string.Empty;
                string text = string.Empty;
                if (inputValue < 0.0)
                {
                    arg = "-";
                    inputValue = 0.0 - inputValue;
                }

                if (format.ToUpper().StartsWith("N"))
                {
                    text = inputValue.ToString(format);
                    text = text.Replace(',', digitSeparator);
                    text = text.Replace('.', decimalSeparator);
                    return $"{arg}{text}";
                }

                string[] array = format.Split(decimalSeparator);
                string text2;
                string text3;
                if (array.Length >= 2)
                {
                    text2 = array[0];
                    text3 = array[1];
                }
                else
                {
                    text2 = format;
                    text3 = string.Empty;
                }

                string text4 = string.Empty;
                array = inputValue.ToString("0.00000000").Split('.');
                string text5;
                if (array.Length >= 2)
                {
                    text5 = array[0];
                    text4 = array[1];
                }
                else
                {
                    text5 = array[0];
                }

                int num = text5.Length;
                array = text2.Split(',');
                while (num > 0)
                {
                    for (int num2 = array.Length - 1; num2 >= 0; num2--)
                    {
                        if (num > 0)
                        {
                            int length = array[num2].Length;
                            num -= length;
                            string text7;
                            if (num < text5.Length && num > 0)
                            {
                                string text6 = text5;
                                int num3 = num;
                                text7 = text6.Substring(num3, text6.Length - num3);
                            }
                            else
                            {
                                text7 = text5;
                            }

                            text5 = ((num <= 0) ? string.Empty : text5.Substring(0, num));
                            text = ((num <= 0) ? $"{text7}{text}" : $"{digitSeparator}{text7}{text}");
                        }
                    }
                }

                if (text4.Length > 0 && text3.Length > 0)
                {
                    text = ((text4.Length <= text3.Length) ? $"{text}{decimalSeparator}{Convert.ToInt32(text4).ToString(text3)}" : $"{text}{decimalSeparator}{text4.Substring(0, text3.Length)}");
                }

                return $"{arg}{text}";
            }

            private static readonly Regex DecimalRegexInstance = new("[^0-9\\.\\-\\+]", RegexOptions.Compiled);
            private static readonly Regex CurrencyRegexInstance = new("[^$0-9\\,\\.\\-\\+]", RegexOptions.Compiled);

            private static Regex DecimalRegex()
            {
                return DecimalRegexInstance;
            }

            private static Regex CurrencyRegex()
            {
                return CurrencyRegexInstance;
            }
        }

        public class CipherFunctions
        {
            public static string Encrypt(string data)
            {
                try
                {
                    return Encryption.Encrypt("CeL.DhK.LaL1@3$5", data);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            public static string Encrypt(string key, string data)
            {
                try
                {
                    return Encryption.Encrypt(key, data);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            public static string Decrypt(string data)
            {
                try
                {
                    return Encryption.Decrypt("CeL.DhK.LaL1@3$5", data);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            public static string Decrypt(string key, string data)
            {
                try
                {
                    return Encryption.Decrypt(key, data);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            public static string EncryptByRSA(string xmlPublicKey, string data)
            {
                try
                {
                    return Encryption.EncryptByRSA(xmlPublicKey, data);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            public static string EncryptByTDS(string data)
            {
                try
                {
                    return TDSEncryption.Encrypt("CeL.DhK.LaL1@3$5", "CoMEaLtD", data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByTDS(string privateKey, string publicKey, string data)
            {
                try
                {
                    return TDSEncryption.Encrypt(privateKey, publicKey, data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByTDS(string privateKey, string publicKey, string data, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 1)
            {
                try
                {
                    return TDSEncryption.Encrypt(privateKey, publicKey, data, keySize, mode, padding, output);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByTDS(string data)
            {
                try
                {
                    return TDSEncryption.Decrypt("CeL.DhK.LaL1@3$5", "CoMEaLtD", data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByTDS(string privateKey, string publicKey, string data)
            {
                try
                {
                    return TDSEncryption.Decrypt(privateKey, publicKey, data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByTDS(string privateKey, string publicKey, string data, int input = 3, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 3)
            {
                try
                {
                    return TDSEncryption.Decrypt(privateKey, publicKey, data, input, keySize, mode, padding, output);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByAES(string data)
            {
                try
                {
                    return AESEncryption.Encrypt("CeL.DhK.LaL1@3$5", "CoMEaLtD", data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByAES(string privateKey, string publicKey, string data)
            {
                try
                {
                    return AESEncryption.Encrypt(privateKey, publicKey, data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByAES(string privateKey, string publicKey, string data, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 1)
            {
                try
                {
                    return AESEncryption.Encrypt(privateKey, publicKey, data, keySize, mode, padding, output);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByAES(string data)
            {
                try
                {
                    return AESEncryption.Decrypt("CeL.DhK.LaL1@3$5", "CoMEaLtD", data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByAES(string privateKey, string publicKey, string data)
            {
                try
                {
                    return AESEncryption.Decrypt(privateKey, publicKey, data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string DecryptByAES(string privateKey, string publicKey, string data, int input = 3, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 3)
            {
                try
                {
                    return AESEncryption.Decrypt(privateKey, publicKey, data, input, keySize, mode, padding, output);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptByMD5(string data)
            {
                try
                {
                    return MD5Encryption.Encrypt(data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptBySHA1(string data)
            {
                try
                {
                    return SHA1Encryption.Encrypt(data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptBySHA256(string data)
            {
                try
                {
                    return SHA256Encryption.Encrypt(data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptBySHA384(string data)
            {
                try
                {
                    return SHA384Encryption.Encrypt(data);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public static string EncryptBySHA512(string data)
            {
                try
                {
                    return SHA256Encryption.Encrypt(data);
                }
                catch (Exception)
                {
                    throw;
                }
            }
    }

        private Global()
        {
        }
    }
}
