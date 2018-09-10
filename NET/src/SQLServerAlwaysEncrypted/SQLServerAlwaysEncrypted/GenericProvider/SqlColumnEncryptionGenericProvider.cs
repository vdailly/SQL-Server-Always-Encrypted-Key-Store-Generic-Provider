using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SQLServerAlwaysEncrypted
{
    /// <summary>
    /// A generic provider whose <see cref="SqlColumnEncryptionCustomProvider.ProviderName"/> is by default set to "GENERIC".
    /// Wrap a regular <see cref="System.Data.SqlClient.SqlColumnEncryptionKeyStoreProvider"/> provider.
    /// Register it with <see cref="SqlConnection.RegisterColumnEncryptionKeyStoreProviders(IDictionary{string, SqlColumnEncryptionKeyStoreProvider})"/>
    /// </summary>
    public class SqlColumnEncryptionGenericProvider : SqlColumnEncryptionKeyStoreProvider
    {
        public static string ProviderName = "GENERIC";
        public string MasterKeyPath = "";

        private SqlColumnEncryptionKeyStoreProvider _provider;

        public SqlColumnEncryptionGenericProvider(SqlColumnEncryptionKeyStoreProvider provider, string masterKeyPath)
        {
            _provider = provider;
            MasterKeyPath = masterKeyPath;
        }

        /// <summary>
        /// When the driver attempt to access the CMK, replace the <paramref name="masterKeyPath"/> from 
        /// the generic provider with the real <see cref="MasterKeyPath"/> of our wrapped provider.
        /// </summary>
        public override byte[] DecryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] encryptedColumnEncryptionKey)
        {
            return _provider.DecryptColumnEncryptionKey(MasterKeyPath, encryptionAlgorithm, encryptedColumnEncryptionKey);
        }

        /// <summary>
        /// When the driver attempt to access the CMK, replace the <paramref name="masterKeyPath"/> from 
        /// the generic provider with the real <see cref="MasterKeyPath"/> of our wrapped provider.
        /// </summary>
        public override byte[] EncryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] columnEncryptionKey)
        {
            return _provider.EncryptColumnEncryptionKey(MasterKeyPath, encryptionAlgorithm, columnEncryptionKey);
        }

    }
}
