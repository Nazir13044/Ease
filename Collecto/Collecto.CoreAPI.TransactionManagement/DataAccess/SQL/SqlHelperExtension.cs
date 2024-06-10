using Collecto.CoreAPI.TransactionManagement.Helper;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.SQL
{
    public sealed class SqlHelperExtension
    {
        private SqlHelperExtension()
        {
        }

        public static void Fill(DbDataReader dataReader, DataSet dataSet, string tableName, int from, int count)
        {
            DataAccessHelper.Fill(dataReader, dataSet, tableName, from, count);
        }

        public static void Fill(DbDataReader dataReader, DataSet dataSet, string tableName)
        {
            Fill(dataReader, dataSet, tableName, 0, 0);
        }

        public static SqlParameter CreateParam(SqlDbType pType, ParameterDirection direction, object pValue)
        {
            return CreateParam(string.Empty, pType, direction, pValue);
        }

        public static SqlParameter CreateParam(string pName, SqlDbType pType, ParameterDirection direction)
        {
            return CreateParam(pName, pType, direction, null);
        }

        public static SqlParameter CreateParam(string pName, SqlDbType pType, ParameterDirection direction, object pValue)
        {
            SqlParameter sqlParameter = new SqlParameter(pName, pType)
            {
                Direction = direction
            };
            if (pType == SqlDbType.VarChar)
            {
                sqlParameter.Size = 1000;
            }

            sqlParameter.Value = pValue;
            return sqlParameter;
        }

        public static SqlParameter CreateParam(string pName, SqlDbType pType, ParameterDirection direction, object pValue, int size)
        {
            SqlParameter sqlParameter = new SqlParameter(pName, pType)
            {
                Direction = direction,
                Size = size
            };
            if ((pType == SqlDbType.Char || pType == SqlDbType.VarChar || pType == SqlDbType.Text || pType == SqlDbType.NChar || pType == SqlDbType.NText || pType == SqlDbType.NVarChar) && size > 0)
            {
                string inputString = (Global.StringFunctions.IsEmptyOrNull(pValue) ? string.Empty : pValue.ToString());
                sqlParameter.Value = Global.StringFunctions.Left(inputString, size);
            }
            else
            {
                sqlParameter.Value = pValue;
            }

            return sqlParameter;
        }

        public static SqlParameter CreateInParam(SqlDbType pType, object pValue)
        {
            return CreateInParam(string.Empty, pType, pValue);
        }

        public static SqlParameter CreateInParam(string pName, SqlDbType pType)
        {
            return CreateInParam(pName, pType, null);
        }

        public static SqlParameter CreateInParam(string pName, SqlDbType pType, object pValue)
        {
            SqlParameter sqlParameter = new SqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Input
            };
            if (pType == SqlDbType.VarChar)
            {
                sqlParameter.Size = 1000;
            }

            sqlParameter.Value = pValue;
            return sqlParameter;
        }

        public static SqlParameter CreateInParam(string pName, SqlDbType pType, object pValue, int size)
        {
            SqlParameter sqlParameter = new SqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Size = size
            };
            if ((pType == SqlDbType.Char || pType == SqlDbType.VarChar || pType == SqlDbType.Text || pType == SqlDbType.NChar || pType == SqlDbType.NVarChar || pType == SqlDbType.NText) && !Global.StringFunctions.IsEmptyOrNull(pValue) && size > 0 && pValue.ToString().Length > size)
            {
                sqlParameter.Value = Global.StringFunctions.Left(pValue.ToString(), size);
            }
            else
            {
                sqlParameter.Value = pValue;
            }

            return sqlParameter;
        }

        public static SqlParameter CreateOutParam(SqlDbType pType, object pValue)
        {
            return CreateOutParam(string.Empty, pType, pValue);
        }

        public static SqlParameter CreateOutParam(string pName, SqlDbType pType)
        {
            return CreateOutParam(pName, pType, null);
        }

        public static SqlParameter CreateOutParam(string pName, SqlDbType pType, object pValue)
        {
            SqlParameter sqlParameter = new SqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Output
            };
            if (pType == SqlDbType.VarChar)
            {
                sqlParameter.Size = 1000;
            }

            sqlParameter.Value = pValue;
            return sqlParameter;
        }

        public static SqlParameter CreateOutParam(string pName, SqlDbType pType, object pValue, int size)
        {
            return new SqlParameter(pName, pType)
            {
                Direction = ParameterDirection.Output,
                Size = size,
                Value = pValue
            };
        }

        public static SqlCommand CreateCommand(string name)
        {
            return new SqlCommand(name);
        }

        public static void AddParameter(SqlCommand command, string name, SqlDbType type, ParameterDirection direction, object value)
        {
            SqlParameter sqlParameter = command.CreateParameter();
            sqlParameter.ParameterName = name;
            sqlParameter.SqlDbType = type;
            sqlParameter.Direction = direction;
            if (type == SqlDbType.VarChar)
            {
                sqlParameter.Size = 1000;
            }

            sqlParameter.Value = value;
            command.Parameters.Add(sqlParameter);
        }

        public static void AddParameter(SqlCommand command, string name, SqlDbType type, ParameterDirection direction)
        {
            AddParameter(command, name, type, direction, null);
        }
    }
}
