using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Oracle
{
    public sealed class OracleHelperExtension
    {
        private OracleHelperExtension()
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

        public static OracleParameter CreateParam(OracleDbType pType, ParameterDirection direction, object pValue)
        {
            return CreateParam(string.Empty, pType, direction, pValue);
        }

        public static OracleParameter CreateParam(string pName, OracleDbType pType, ParameterDirection direction)
        {
            return CreateParam(pName, pType, direction, null);
        }

        public static OracleParameter CreateParam(string pName, OracleDbType pType, ParameterDirection direction, object pValue)
        {
            OracleParameter oracleParameter = new OracleParameter(pName, pType)
            {
                Direction = direction,
                Value = pValue
            };
            if (pType == OracleDbType.Varchar2)
            {
                oracleParameter.Size = 1000;
            }

            return oracleParameter;
        }

        public static OracleParameter CreateParam(string pName, OracleDbType pType, ParameterDirection direction, int size, object pValue)
        {
            return new OracleParameter(pName, pType)
            {
                Direction = direction,
                Size = size,
                Value = pValue
            };
        }

        public static OracleParameter CreateInParam(OracleDbType pType, object pValue)
        {
            return CreateInParam(string.Empty, pType, pValue);
        }

        public static OracleParameter CreateInParam(string pName, OracleDbType pType)
        {
            return CreateInParam(pName, pType, null);
        }

        public static OracleParameter CreateInParam(string pName, OracleDbType pType, object pValue)
        {
            OracleParameter oracleParameter = new OracleParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Value = pValue
            };
            if (pType == OracleDbType.Varchar2)
            {
                oracleParameter.Size = 1000;
            }

            return oracleParameter;
        }

        public static OracleParameter CreateInParam(string pName, OracleDbType pType, object pValue, int size)
        {
            return new OracleParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Size = size,
                Value = pValue
            };
        }

        public static OracleParameter CreateOutParam(OracleDbType pType, object pValue)
        {
            return CreateOutParam(string.Empty, pType, pValue);
        }

        public static OracleParameter CreateOutParam(string pName, OracleDbType pType)
        {
            return CreateOutParam(pName, pType, null);
        }

        public static OracleParameter CreateOutParam(string pName, OracleDbType pType, object pValue)
        {
            OracleParameter oracleParameter = new OracleParameter(pName, pType)
            {
                Value = pValue,
                Direction = ParameterDirection.Output
            };
            if (pType == OracleDbType.Varchar2)
            {
                oracleParameter.Size = 1000;
            }

            return oracleParameter;
        }

        public static OracleParameter CreateOutParam(string pName, OracleDbType pType, object pValue, int size)
        {
            return new OracleParameter(pName, pType)
            {
                Direction = ParameterDirection.Output,
                Size = size,
                Value = pValue
            };
        }

        public static OracleCommand CreateCommand(string name)
        {
            return new OracleCommand(name);
        }

        public static void AddParameter(OracleCommand command, string name, OracleDbType type, ParameterDirection direction, object Value)
        {
            OracleParameter oracleParameter = command.CreateParameter();
            oracleParameter.ParameterName = name;
            oracleParameter.OracleDbType = type;
            oracleParameter.Direction = direction;
            if (type == OracleDbType.Varchar2)
            {
                oracleParameter.Size = 4000;
            }

            oracleParameter.Value = Value;
            command.Parameters.Add(oracleParameter);
        }

        public static void AddParameter(OracleCommand command, string name, OracleDbType type, ParameterDirection direction)
        {
            AddParameter(command, name, type, direction, null);
        }
    }
}
