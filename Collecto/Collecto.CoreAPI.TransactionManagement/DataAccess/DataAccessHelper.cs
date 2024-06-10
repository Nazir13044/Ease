using System.Data;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    internal class DataAccessHelper
    {
        public static void Fill(IDataReader dataReader, DataSet dataSet, string tableName, int from, int count)
        {
            int num = 0;
            int num2 = from + count;
            if (tableName == null)
            {
                tableName = "unknownTable";
            }

            if (dataSet.Tables[tableName] == null)
            {
                dataSet.Tables.Add(tableName);
            }

            DataTable dataTable = ((tableName != null) ? dataSet.Tables[tableName] : dataSet.Tables[0]);
            while (dataReader.Read())
            {
                if (num++ >= from)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        string name = dataReader.GetName(i);
                        Type type = dataReader.GetValue(i).GetType();
                        if (dataTable.Columns.IndexOf(name) == -1)
                        {
                            dataTable.Columns.Add(name, type);
                        }

                        dataRow[name] = GetValue(dataReader.GetValue(i), type);
                    }

                    dataTable.Rows.Add(dataRow);
                }

                if (count != 0 && num2 <= num)
                {
                    break;
                }
            }

            dataSet.AcceptChanges();
        }

        public static void Fill(IDataReader dataReader, DataSet dataSet, string tableName)
        {
            Fill(dataReader, dataSet, tableName, 0, 0);
        }

        private static object GetValue(object fieldValue, Type fieldType)
        {
            switch (fieldType.Name)
            {
                case "Int16":
                case "Int32":
                case "Int64":
                case "UInt16":
                case "UInt32":
                case "UInt64":
                    if (fieldValue != DBNull.Value)
                    {
                        return fieldValue;
                    }

                    return 0;
                default:
                    return fieldValue;
            }
        }
    }
}
