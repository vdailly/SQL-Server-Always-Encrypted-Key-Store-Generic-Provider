using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections.ObjectModel;

namespace SQLServerAlwaysEncrypted
{
    public static class SqlConnectionExtension
    {

        private static FieldInfo sqlColumnCustomProvidersField = typeof(SqlConnection).GetField("_CustomColumnEncryptionKeyStoreProviders",
                                                BindingFlags.Static | BindingFlags.NonPublic);
        /// <summary>
        /// SqlConnection.RegisterColumnEncryptionKeyStoreProviders() use a private field "_CustomColumnEncryptionKeyStoreProviders"
        /// to store the provider. But when using the PowerShell SqlServer module, the module already register a provider 
        /// so that further call to this method fails. So to add a new custom provider, access to the private underlying field is required.
        /// </summary>
        public static Dictionary<string, SqlColumnEncryptionKeyStoreProvider> GetCustomKeyStoreProvider()
        {
            
            if (null != sqlColumnCustomProvidersField)
            {
                return new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(
                    (ReadOnlyDictionary<string, SqlColumnEncryptionKeyStoreProvider>)sqlColumnCustomProvidersField.GetValue(typeof(SqlConnection))
                );
            }
            return null;
        }

        public static void SetCustomKeyStoreProvider(Dictionary<string, SqlColumnEncryptionKeyStoreProvider> sqlColumnCustomProviders)
        {
            if (null != sqlColumnCustomProviders)
            {
                sqlColumnCustomProvidersField.SetValue(typeof(SqlConnection),
                    new ReadOnlyDictionary<string, SqlColumnEncryptionKeyStoreProvider>(sqlColumnCustomProviders)
                );
            }
        }
    }
}
