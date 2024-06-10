using System.Data.SqlClient;

namespace Collecto.CoreAPI.Services
{
    internal static class DBCustomError
    {
        internal static Exception GenerateCustomError(Exception e)
        {
            Exception customError;
            if (e is SqlException)
            {
                SqlException se = e as SqlException;
                if (se.Number == -1 || se.Number == 2 || se.Number == 53 || se.Number == 10060)
                {
                    customError = new Exception("SQL Server/Database is invalid.", e);
                }
                else if (se.Number == -2)
                {
                    customError = new Exception(se.InnerException != null ? se.InnerException.Message : "Server is too busy to respond.", e);
                }
                else if (se.Number == 207)
                {
                    int startIdx = se.Message.IndexOf("'");
                    if (startIdx > 0)
                    {
                        int endIdx = se.Message.IndexOf("'", startIdx + 1);
                        if (endIdx > startIdx && endIdx > 0)
                        {
                            string fldName = se.Message.Substring(startIdx + 1, (endIdx - startIdx - 1));
                            customError = new Exception($"Column name ({fldName}) in the Table is invalid.", e);
                        }
                        else
                        {
                            customError = new Exception("Column name in the Table is invalid.", e);
                        }
                    }
                    else
                    {
                        customError = new Exception("Column name in the Table is invalid.", e);
                    }
                }
                else if (se.Number == 1205)
                {
                    customError = new Exception("Server is too busy to process your request.", e);
                }
                else if (se.Number == 547)
                {
                    int startIdx = e.Message.IndexOf("conflicted with the FOREIGN KEY constraint \"");
                    if (startIdx >= 0)
                    {
                        startIdx = e.Message.IndexOf(", table \"");
                        startIdx += 13;
                        int endIdx = e.Message.IndexOf("\"", startIdx);
                        if (endIdx >= 0)
                        {
                            string tableName = e.Message[startIdx..endIdx];
                            customError = new Exception($"Foreign key constraint violation occurred in Table [<b>{tableName}</b>].", e);
                        }
                        else
                        {
                            customError = new Exception("Foreign key constraint violation occurred", e);
                        }
                    }
                    else
                    {
                        customError = new Exception("Can not <b>Delete</b> data because it is already used as reference.", e);
                    }
                }
                else if (se.Number == 2601 || se.Number == 2627)
                {
                    int startIdx = e.Message.IndexOf("The duplicate key value is (");
                    if (startIdx >= 0)
                    {
                        string fldName = string.Empty;
                        int stIdx = e.Message.IndexOf("with unique index '");
                        stIdx += 19;
                        int endIdx = e.Message.IndexOf("'", stIdx);
                        if (endIdx > 0 && stIdx > 0)
                        {
                            fldName = e.Message[stIdx..endIdx];
                            stIdx = fldName.IndexOf("_");
                            if (stIdx >= 0)
                            {
                                fldName = fldName[(stIdx + 1)..];
                                string[] tokens = fldName.Split('_');
                                if (tokens.Length >= 2)
                                {
                                    fldName = $" Table [<b>{tokens[0]}</b>] and Field [<b>{tokens[1]}</b>]";
                                }
                            }
                        }

                        startIdx += 28;
                        endIdx = e.Message.IndexOf(")", startIdx);
                        if (endIdx >= 0)
                        {
                            string value = e.Message[startIdx..endIdx];
                            customError = new Exception($"Value [<b>{value}</b>] is already exist in {fldName}.", e);
                        }
                        else
                        {
                            customError = new Exception("Value can not be duplicate in the system.", e);
                        }
                    }
                    else
                    {
                        customError = new Exception("Value can not be duplicate in the system", e);
                    }
                }
                else
                {
                    customError = new Exception(se.Message, e);
                }
            }
            else
            {
                customError = new Exception(e.Message, e);
            }

            return customError;
        }
    }
}
