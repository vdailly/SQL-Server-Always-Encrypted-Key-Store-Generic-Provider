using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.AlwaysEncrypted.Management;
using System.Reflection;

namespace SQLServerAlwaysEncrypted.Cmdlets
{

    [Cmdlet(VerbsLifecycle.Register, "SqlColumnEncryptionCustomProvider")]
    [CmdletBinding()]
    public class RegisterSqlColumnEncryptionGenericProvider : Cmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNull()]
        public SqlColumnEncryptionKeyStoreProvider Provider { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string ProviderName { get; set; }

        protected override void ProcessRecord()
        {
            if (null != AlwaysEncryptedManagement.CustomProviders)
            {
                AlwaysEncryptedManagement.CustomProviders.Add(this.ProviderName, this.Provider);
            }

            //retrieve the provider name we want to register.
            //don't know why Microsoft use a static field for their provider name (i.e. SqlColumnEncryptionCertificateStoreProvider.ProviderName)
            //especially because the base class SqlColumnEncryptionKeyStoreProvider doesn't provide an abstract static field ProviderName.
            //to keep consistent with their implementation, i use also a static field.
            //the JDBC driver use a non-static field.
            var providerName = string.Empty;
            {
                var providerType = Provider.GetType();
                providerName = (string)providerType.GetField("ProviderName", BindingFlags.Static | BindingFlags.Public)
                                                   .GetValue(providerType);
            }

            //register the custom provider in SqlConnection class
            var sqlColumnCustomProviders = SqlConnectionExtension.GetCustomKeyStoreProvider();
            if (null != sqlColumnCustomProviders)
            {
                sqlColumnCustomProviders.Add(providerName, Provider);
            }
        }
    }


    [Cmdlet(VerbsCommon.Get, "SqlColumnEncryptionCustomProvider")]
    [CmdletBinding()]
    public class GetSqlColumnEncryptionCustomProvider : Cmdlet
    {
        [Parameter]
        public SwitchParameter FromSQLConnection { get; set; }

        protected override void ProcessRecord()
        {
            if (FromSQLConnection.IsPresent)
            {
                var customProviders = SqlConnectionExtension.GetCustomKeyStoreProvider();
                WriteObject(
                    new Dictionary<String, SqlColumnEncryptionKeyStoreProvider>(customProviders)
                );
            }
            else
            {
                WriteObject(
                    new Dictionary<String, SqlColumnEncryptionKeyStoreProvider>(AlwaysEncryptedManagement.CustomProviders)
                );
            }

        }
    }

}
