using System.Data;
using System.Data.Common;
using System.Data.Odbc;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc
{
    public sealed class OdbcHelperExtension
    {
        private OdbcHelperExtension()
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

        public static OdbcParameter CreateParam(OdbcType pType, ParameterDirection direction, object pValue)
        {
            return CreateParam(string.Empty, pType, direction, pValue);
        }

        public static OdbcParameter CreateParam(string pName, OdbcType pType, ParameterDirection direction)
        {
            return CreateParam(pName, pType, direction, null);
        }

        public static OdbcParameter CreateParam(string pName, OdbcType pType, ParameterDirection direction, object pValue)
        {
            OdbcParameter odbcParameter = new OdbcParameter(pName, pType)
            {
                Direction = direction,
                Value = pValue
            };
            if (pType == OdbcType.VarChar)
            {
                odbcParameter.Size = 1000;
            }

            return odbcParameter;
        }

        public static OdbcParameter CreateParam(string pName, OdbcType pType, ParameterDirection direction, object pValue, int size)
        {
            return new OdbcParameter(pName, pType)
            {
                Direction = direction,
                Size = size,
                Value = pValue
            };
        }

        public static OdbcParameter CreateInParam(OdbcType pType, object pValue)
        {
            return CreateInParam(string.Empty, pType, pValue);
        }

        public static OdbcParameter CreateInParam(string pName, OdbcType pType)
        {
            return CreateInParam(pName, pType, null);
        }

        public static OdbcParameter CreateInParam(string pName, OdbcType pType, object pValue)
        {
            OdbcParameter odbcParameter = new OdbcParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Value = pValue
            };
            if (pType == OdbcType.VarChar)
            {
                odbcParameter.Size = 1000;
            }

            return odbcParameter;
        }

        public static OdbcParameter CreateInParam(string pName, OdbcType pType, object pValue, int size)
        {
            return new OdbcParameter(pName, pType)
            {
                Direction = ParameterDirection.Input,
                Size = size,
                Value = pValue
            };
        }

        public static OdbcParameter CreateOutParam(OdbcType pType, object pValue)
        {
            return CreateOutParam(string.Empty, pType, pValue);
        }

        public static OdbcParameter CreateOutParam(string pName, OdbcType pType)
        {
            return CreateOutParam(pName, pType, null);
        }

        public static OdbcParameter CreateOutParam(string pName, OdbcType pType, object pValue)
        {
            OdbcParameter odbcParameter = new OdbcParameter(pName, pType)
            {
                Direction = ParameterDirection.Output,
                Value = pValue
            };
            if (pType == OdbcType.VarChar)
            {
                odbcParameter.Size = 1000;
            }

            return odbcParameter;
        }

        public static OdbcParameter CreateOutParam(string pName, OdbcType pType, object pValue, int size)
        {
            return new OdbcParameter(pName, pType)
            {
                Direction = ParameterDirection.Output,
                Size = size,
                Value = pValue
            };
        }

        public static OdbcCommand CreateCommand(string name)
        {
            return new OdbcCommand(name);
        }

        public static void AddParameter(OdbcCommand command, string name, OdbcType type, ParameterDirection direction, object value)
        {
            OdbcParameter odbcParameter = command.CreateParameter();
            odbcParameter.ParameterName = name;
            odbcParameter.OdbcType = type;
            odbcParameter.Direction = direction;
            if (type == OdbcType.VarChar)
            {
                odbcParameter.Size = 1000;
            }

            odbcParameter.Value = value;
            command.Parameters.Add(odbcParameter);
        }

        public static void AddParameter(OdbcCommand command, string name, OdbcType type, ParameterDirection direction)
        {
            AddParameter(command, name, type, direction, null);
        }
    }
}
