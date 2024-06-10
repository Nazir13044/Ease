using System.Collections;
using System.Data;
using System.Data.Odbc;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc
{
    internal sealed class OdbcHelperParameterCache
    {
        private static readonly Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private OdbcHelperParameterCache()
        {
        }

        private static OdbcParameter[] DiscoverSpParameterSet(OdbcConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            using OdbcCommand odbcCommand = new OdbcCommand(spName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            connection.Open();
            connection.Close();
            if (!includeReturnValueParameter)
            {
                odbcCommand.Parameters.RemoveAt(0);
            }

            OdbcParameter[] array = new OdbcParameter[odbcCommand.Parameters.Count];
            odbcCommand.Parameters.CopyTo(array, 0);
            OdbcParameter[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i].Value = DBNull.Value;
            }

            return array;
        }

        private static OdbcParameter[] CloneParameters(OdbcParameter[] originalParameters)
        {
            OdbcParameter[] array = new OdbcParameter[originalParameters.Length];
            int i = 0;
            for (int num = originalParameters.Length; i < num; i++)
            {
                array[i] = (OdbcParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return array;
        }

        public static void CacheParameterSet(string connectionString, string commandText, params OdbcParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            string key = connectionString + ":" + commandText;
            paramCache[key] = commandParameters;
        }

        public static OdbcParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            string key = connectionString + ":" + commandText;
            if (!(paramCache[key] is OdbcParameter[] originalParameters))
            {
                return null;
            }

            return CloneParameters(originalParameters);
        }

        public static OdbcParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, includeReturnValueParameter: false);
        }

        public static OdbcParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            using OdbcConnection connection = new OdbcConnection(connectionString);
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }

        internal static OdbcParameter[] GetSpParameterSet(OdbcConnection connection, string spName)
        {
            return GetSpParameterSet(connection, spName, includeReturnValueParameter: false);
        }

        internal static OdbcParameter[] GetSpParameterSet(OdbcConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using OdbcConnection connection2 = (OdbcConnection)((ICloneable)connection).Clone();
            return GetSpParameterSetInternal(connection2, spName, includeReturnValueParameter);
        }

        private static OdbcParameter[] GetSpParameterSetInternal(OdbcConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            string key = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");
            OdbcParameter[] array = paramCache[key] as OdbcParameter[];
            if (array == null)
            {
                OdbcParameter[] array2 = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
                paramCache[key] = array2;
                array = array2;
            }

            return CloneParameters(array);
        }
    }
}
