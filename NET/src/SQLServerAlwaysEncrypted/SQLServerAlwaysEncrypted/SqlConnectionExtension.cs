using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace SQLServerAlwaysEncrypted
{
    public static class SqlConnectionExtension
    {
        /// <summary>
        /// SqlConnection.RegisterColumnEncryptionKeyStoreProviders() use a private field "_CustomColumnEncryptionKeyStoreProviders"
        /// to store the provider. But when using the PowerShell SqlServer module, the module already register a provider 
        /// so that further call to this method fails. So to add a new custom provider, access to the private underlying field is required.
        /// </summary>
        public static Dictionary<string, SqlColumnEncryptionKeyStoreProvider> GetCustomKeyStoreProvider()
        {
            var sqlColumnCustomProvidersField = typeof(SqlConnection).GetField("_CustomColumnEncryptionKeyStoreProviders",
                                                BindingFlags.Static | BindingFlags.NonPublic);

            if (null != sqlColumnCustomProvidersField)
            {
                return (Dictionary<string, SqlColumnEncryptionKeyStoreProvider>)sqlColumnCustomProvidersField.GetValue(typeof(SqlConnection));
            }
            return null;
        }
    }
}
