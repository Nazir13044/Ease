using Collecto.CoreAPI.Models.Global;
using Collecto.CoreAPI.Models.Objects.Setups;
using Collecto.CoreAPI.Models.Requests.Setups;
using Collecto.CoreAPI.Models.Responses.Setups;
using Collecto.CoreAPI.Services.Contracts.Setups;
using Collecto.CoreAPI.TransactionManagement.DataAccess;
using Collecto.CoreAPI.TransactionManagement.DataAccess.SQL;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.Services.Services.Setups
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings"></param>
    public class AuthModulesService(IOptions<AppSettings> settings) : IAuthModulesService
    {
        private readonly AppSettings _settings = settings.Value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="subsystemId"></param>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <param name="entryModule"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AuthSummariesResponse> GetAuthSummariesAsync(int systemId, int subsystemId, int userId, short status, int entryModule)
        {
            AuthSummariesResponse response = new() { Value = [] };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@SystemId", pType: SqlDbType.Int, pValue: systemId),
                        SqlHelperExtension.CreateInParam(pName: "@SubsystemId", pType: SqlDbType.Int, pValue: subsystemId),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.Int, pValue: status),
                        SqlHelperExtension.CreateInParam(pName: "@EntryModule", pType: SqlDbType.Int, pValue: entryModule)
                    ];
                    using (IDataReader dr = tc.ExecuteReaderSp(spName: "dbo.GetPendingData", parameterValues: p))
                    {
                        while (dr.Read())
                        {
                            AuthModules item = new()
                            {
                                ModuleName = dr.GetString(0),
                                ModuleId = dr.GetString(1),
                                PendingItems = dr.GetInt32(2)
                            };
                            response.Value.Add(item);
                        }
                        dr.Close();
                    }

                    tc.End();
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="systemId"></param>
        /// <param name="subsystemId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<AuthDetailsResponse> GetAuthDetailsAsync(string moduleId, int systemId, int subsystemId, short status)
        {
            AuthDetailsResponse response = new() { Columns = [], Data = [] };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@ModuleId", pType: SqlDbType.VarChar, pValue: moduleId, size: 25),
                        SqlHelperExtension.CreateInParam(pName: "@SystemId", pType: SqlDbType.Int, pValue: systemId),
                        SqlHelperExtension.CreateInParam(pName: "@SubsystemId", pType: SqlDbType.Int, pValue: subsystemId),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.Int, pValue: status)
                    ];

                    using (IDataReader dr = tc.ExecuteReaderSp(spName: "dbo.GetAuthProcessDetail", parameterValues: p.ToArray()))
                    {
                        //Read Columns
                        for (int fldIdx = 0; fldIdx < dr.FieldCount; fldIdx++)
                        {
                            string[] fldParam = dr.GetName(fldIdx).Split('~');

                            AuthModuleColumn column = new() { Field = fldParam[0], Title = fldParam[1], Width = Convert.ToInt32(fldParam[2]) };
                            column.Attributes = column.Width > 0 ? $"{column.Width}" : "auto";

                            response.Columns.Add(column);
                        }

                        //Read Data 
                        while (dr.Read())
                        {
                            Dictionary<string, object> row = new();
                            for (int fldIdx = 0; fldIdx < dr.FieldCount; fldIdx++)
                            {
                                object value = dr[fldIdx];
                                string key = response.Columns[fldIdx].Field;

                                row[key] = value;
                            }
                            response.Data.Add(row);
                        }

                        dr.Close();
                    }

                    tc.End();
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="ipAddress"></param>
        /// <param name="remarks"></param>
        /// <param name="status"></param>
        /// <param name="userId"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAuthStatusAsync(string moduleId, string ipAddress, string remarks, short status, int userId, string ids)
        {
            bool returnValue;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@ModuleId", pType: SqlDbType.VarChar, pValue: moduleId, size: 25),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                        SqlHelperExtension.CreateInParam(pName: "@Remarks", pType: SqlDbType.VarChar, pValue: remarks, size: 50),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.SmallInt, pValue: status),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@Ids", pType: SqlDbType.VarChar, pValue: ids, size: 7500),
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.UpdateAuthStatus", parameterValues: p);

                    tc.End();

                    returnValue = true;
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="subsystemId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AuthModulesBasicResponse> GetAuthModulesBasicAsync()
        {
            AuthModulesBasicResponse response = new() { Value = [] };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    string whereClause = string.Empty;

                    whereClause = $" {SQLParser.TagSQL(whereClause)}ModuleID IS NOT NULL";

                    using (IDataReader dr = tc.ExecuteReader("SELECT ModuleID, ModuleName FROM LabelSettings%q ORDER BY ModuleName", whereClause))
                    {
                        while (dr.Read())
                        {
                            AuthModuleBasic item = new()
                            {
                                ModuleId = dr.GetString(0),
                                ModuleName = dr.GetString(1),
                            };

                            response.Value.Add(item);
                        }
                        dr.Close();
                    }

                    tc.End();
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="systemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AuthDetailsResponse> GetInactiveDetailsAsync(InactiveDetailRequest request, int systemId, int userId)
        {
            AuthDetailsResponse response = new() { Columns = [], Data = [] };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@ModuleId", pType: SqlDbType.VarChar, pValue: request.ModuleId, size: 25),
                        SqlHelperExtension.CreateInParam(pName: "@UserID", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@SystemId", pType: SqlDbType.Int, pValue: systemId),
                        SqlHelperExtension.CreateInParam(pName: "@SubsystemId", pType: SqlDbType.Int, pValue: request.SubsystemId),
                        SqlHelperExtension.CreateInParam(pName: "@SalesPointID", pType: SqlDbType.Int, pValue: request.SalespointId),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.SmallInt, pValue: request.Status),
                        SqlHelperExtension.CreateInParam(pName: "@SearchText", pType: SqlDbType.VarChar, pValue: request.Criteria, size: 100)
                    ];

                    using (IDataReader dr = tc.ExecuteReaderSp(spName: "dbo.GetInactiveProcessDetail", parameterValues: p))
                    {
                        //Read Columns
                        for (int fldIdx = 0; fldIdx < dr.FieldCount; fldIdx++)
                        {
                            string[] fldParam = dr.GetName(fldIdx).Split('~');

                            AuthModuleColumn column = new() { Field = fldParam[0], Title = fldParam[1], Width = Convert.ToInt32(fldParam[2]) };
                            response.Columns.Add(column);
                        }

                        //Read Data 
                        while (dr.Read())
                        {
                            Dictionary<string, object> row = [];
                            for (int fldIdx = 0; fldIdx < dr.FieldCount; fldIdx++)
                            {
                                object value = dr[fldIdx];
                                string key = response.Columns[fldIdx].Field;

                                row[key] = value;
                            }
                            response.Data.Add(row);
                        }

                        dr.Close();
                    }

                    tc.End();
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return response;
        }
    }
}
