namespace Collecto.CoreAPI.TransactionManagement.Molels
{
    public enum SQLSyntax
    {
        SQL = 1,
        Oracle,
        MySql,
        Informix,
        Access
    }
    public enum Provider
    {
        Sql = 1,
        Oracle,
        Odbc,
        MySql
    }
    public enum FormatOptions : byte
    {
        ddMMyyyy = 1,
        MMddyyyy,
        ddMMMyyyy
    }

}
