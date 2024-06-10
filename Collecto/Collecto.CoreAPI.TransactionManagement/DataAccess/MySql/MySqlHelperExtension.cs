using Collecto.CoreAPI.TransactionManagement.Helper;
using MySql.Data.MySqlClient;
using System.Data;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.MySql
{
    public sealed class MySqlHelperExtension
    {
        private MySqlHelperExtension()
        {
        }

        public static void Fill(MySqlDataReader dataReader, DataSet dataSet, string tableName, int from, int count)
        {
            DataAccessHelper.Fill(dataReader, dataSet, tableName, from, count);
        }

        public static void Fill(MySqlDataReader dataReader, DataSet dataSet, string tableName)
        {
            Fill(dataReader, dataSet, tableName, 0, 0);
        }

        public static MySqlParameter CreateParam(MySqlDbType pType, ParameterDirection direction, object pValue)
        {
            return CreateParam(string.Empty, pType, direction, pValue);
        }

        public static MySqlParameter CreateParam(string pName, MySqlDbType pType, ParameterDirection direction)
        {
            return CreateParam(pName, pType, direction, null);
        }

        public static MySqlParameter CreateParam(string pName, MySqlDbType pType, ParameterDirection direction, object pValue)
        {
            int num = 0;
            if ((pType == MySqlDbType.VarChar || pType == MySqlDbType.TinyText || pType == MySqlDbType.Text || pType == MySqlDbType.MediumText || pType == MySqlDbType.LongText) && num > 0)
            {
                num = 1000;
            }

            return CreateParam(pName, pType, direction, pValue, num);
        }

        public static MySqlParameter CreateParam(string pName, MySqlDbType pType, ParameterDirection direction, object pValue, int size)
        {
            MySqlParameter mySqlParameter = new MySqlParameter(pName, pType)
            {
                Direction = direction,
                Size = size
            };
            if ((pType == MySqlDbType.VarChar || pType == MySqlDbType.TinyText || pType == MySqlDbType.Text || pType == MySqlDbType.MediumText || pType == MySqlDbType.LongText) && size > 0)
            {
                string inputString = (Global.StringFunctions.IsEmptyOrNull(pValue) ? string.Empty : pValue.ToString());
                mySqlParameter.Value = Global.StringFunctions.Left(inputString, size);
            }
            else
            {
                mySqlParameter.Value = pValue;
            }

            return mySqlParameter;
        }

        public static MySqlParameter CreateInParam(MySqlDbType pType, object pValue)
        {
            return CreateInParam(string.Empty, pType, pValue);
        }

        public static MySqlParameter CreateInParam(string pName, MySqlDbType pType)
        {
            return CreateInParam(pName, pType, null);
        }

        public static MySqlParameter CreateInParam(string pName, MySqlDbType pType, object pValue)
        {
            int num = 0;
            if ((pType == MySqlDbType.VarChar || pType == MySqlDbType.TinyText || pType == MySqlDbType.Text || pType == MySqlDbType.MediumText || pType == MySqlDbType.LongText) && num > 0)
            {
                num = 1000;
            }

            return CreateInParam(pName, pType, pValue, num);
        }

        public static MySqlParameter CreateInParam(string pName, MySqlDbType pType, object pValue, int size)
        {
            MySqlParameter mySqlParameter = new MySqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Size = size
            };
            if ((pType == MySqlDbType.VarChar || pType == MySqlDbType.TinyText || pType == MySqlDbType.Text || pType == MySqlDbType.MediumText || pType == MySqlDbType.LongText) && size > 0)
            {
                string inputString = (Global.StringFunctions.IsEmptyOrNull(pValue) ? string.Empty : pValue.ToString());
                mySqlParameter.Value = Global.StringFunctions.Left(inputString, size);
            }
            else
            {
                mySqlParameter.Value = pValue;
            }

            return mySqlParameter;
        }

        public static MySqlParameter CreateOutParam(MySqlDbType pType, object pValue)
        {
            return CreateOutParam(string.Empty, pType, pValue);
        }

        public static MySqlParameter CreateOutParam(string pName, MySqlDbType pType)
        {
            return CreateOutParam(pName, pType, null);
        }

        public static MySqlParameter CreateOutParam(string pName, MySqlDbType pType, object pValue)
        {
            int num = 0;
            if ((pType == MySqlDbType.VarChar || pType == MySqlDbType.TinyText || pType == MySqlDbType.Text || pType == MySqlDbType.MediumText || pType == MySqlDbType.LongText) && num > 0)
            {
                num = 1000;
            }

            return CreateOutParam(pName, pType, pValue, num);
        }

        public static MySqlParameter CreateOutParam(string pName, MySqlDbType pType, object pValue, int size)
        {
            return new MySqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Output,
                Size = size,
                Value = pValue
            };
        }

        public static MySqlCommand CreateCommand(string name)
        {
            return new MySqlCommand(name);
        }

        public static void AddParameter(MySqlCommand command, string name, MySqlDbType type, ParameterDirection direction, object value)
        {
            MySqlParameter mySqlParameter = command.CreateParameter();
            mySqlParameter.ParameterName = name;
            mySqlParameter.MySqlDbType = type;
            mySqlParameter.Direction = direction;
            if (type == MySqlDbType.VarChar)
            {
                mySqlParameter.Size = 1000;
            }

            mySqlParameter.Value = value;
            command.Parameters.Add(mySqlParameter);
        }

        public static void AddParameter(MySqlCommand command, string name, MySqlDbType type, ParameterDirection direction)
        {
            AddParameter(command, name, type, direction, null);
        }
    }
}
