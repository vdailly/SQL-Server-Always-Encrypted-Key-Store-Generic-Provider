# Already registered custom provider

## Information about the issue

One of the issue encountered during development of this generic provider was the moment where you want to encrypt columns. The cmdlet used to encrypt the columns does use the [System.Data.SqlClient.SqlConnection] object to get access to registered custom providers.

By following the <a href="#callstack">callstack</a> from the load of the PowerShell module, it is clear that both the PowerShell module and the code from [System.Data.SqlClient] present some issues:
- The SqlServer PowerShell module loads the "AZURE_KEY_VAULT" as a custom provider and by doing so prevent any other registration of a custom provider.
- The SqlConnection object class accept only one custom provider registration.



Hopefully, using [System.Reflection] it is easy to circonvert this issue once figured. The extended Always Encrypted cmdlets provided in [SQLServerAlwaysEncrypted.dll](bin\SQLServerAlwaysEncrypted.dll) allow to easily register any additionnal provider. This is the extended provided cmdlet [Register-SqlColumnEncryptionCustomProvider](NET\src\SQLServerAlwaysEncrypted\SQLServerAlwaysEncrypted\Cmdlets\ExtendedCmdlets.cs) which does ensure to register the generic provider in all Dictionnary<String, SqlColumnEncryptionKeyStoreProvider> whenever found in the detailed callstack below.

## <span id="callstack">Call Stack details</span>

The following paragraph provide in-depth object class responsible of this issue, from the load of the Sql Server module.

ILSpy from [System.Data.SqlClient.SqlConnection] class:
```CSharp
using Microsoft.SqlServer.Management.AlwaysEncrypted.Types;

public static void RegisterColumnEncryptionKeyStoreProviders(IDictionary<string, SqlColumnEncryptionKeyStoreProvider> customProviders)
{
	if (customProviders == null)
	{
		throw SQL.NullCustomKeyStoreProviderDictionary();
	}
	foreach (string key in customProviders.Keys)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw SQL.EmptyProviderName();
		}
		if (key.StartsWith("MSSQL_", StringComparison.InvariantCultureIgnoreCase))
		{
			throw SQL.InvalidCustomKeyStoreProviderName(key, "MSSQL_");
		}
		if (customProviders[key] == null)
		{
			throw SQL.NullProviderValue(key);
		}
	}
	lock (_CustomColumnEncryptionKeyProvidersLock)
	{
		if (_CustomColumnEncryptionKeyStoreProviders != null)
		{
			throw SQL.CanOnlyCallOnce();
		}
		Dictionary<string, SqlColumnEncryptionKeyStoreProvider> dictionary = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(customProviders, StringComparer.OrdinalIgnoreCase);
		_CustomColumnEncryptionKeyStoreProviders = new ReadOnlyDictionary<string, SqlColumnEncryptionKeyStoreProvider>(dictionary);
	}
}
```

The main issue comes from the latest lines. The code check if the private field "_CustomColumnEncryptionKeyStoreProviders" is null. Whenever the SqlServer PowerShell module is loaded (and some Always Encrypted cmdlets used) then the module loads the following assemblies in following order:

1. Microsoft.SqlServer.Management.PSSnapins.dll: [Microsoft.SqlServer.Management.PowerShell.AlwaysEncrypted.*]
The following code display the New-SqlColumnEncryptionKeyEncryptedValue cmdlet. We see a call to a[Microsoft.SqlServer.Management.AlwaysEncrypted.Types.AlwaysEncryptedManager] object.

```CSharp
using Microsoft.SqlServer.Management.AlwaysEncrypted.Types;

[Cmdlet("New", "SqlColumnEncryptionKeyEncryptedValue")]
public class NewSqlColumnEncryptionKeyEncryptedValue : Cmdlet {
    protected override void ProcessRecord()
	{
		...
        byte[] hex = AlwaysEncryptedManager.CreateEncryptedValue(targetColumnMasterKeySettings.KeyStoreProviderName, targetColumnMasterKeySettings.KeyPath, AddSqlAzureAuthenticationContext.AzureAuthInfo);
		...
	}
}
```

2. Microsoft.SqlServer.Management.AlwaysEncrypted.Types.dll: [Microsoft.SqlServer.Management.AlwaysEncrypted.Types.AlwaysEncryptedManager]

The [Microsoft.SqlServer.Management.AlwaysEncrypted.Types.AlwaysEncryptedManager] is static and loads with reflection the "Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll" ([the patched dll](bin\Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll)). 

```CSharp
using System.Reflection;
public static class AlwaysEncryptedManager {

    private static Type alwaysEncryptedManagement;
    ...

    ///static constructor
    static AlwaysEncryptedManager() {
        assemblylocation = Assembly.GetExecutingAssembly().Location;
        alwaysEncryptedManagementAssembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(assemblylocation), "Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll"));
        alwaysEncryptedManagement = alwaysEncryptedManagementAssembly.GetType("Microsoft.SqlServer.Management.AlwaysEncrypted.Management.AlwaysEncryptedManagement");
        ...
    }

    public static byte[] CreateEncryptedValue(string, string, ...) {
        ...
        return (byte[])alwaysEncryptedManagement.GetMethod(MethodBase.GetCurrentMethod().Name, types).Invoke(null, parameters);
    }
}
```


3. Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll: [Microsoft.SqlServer.Management.AlwaysEncrypted.AlwaysEncryptedManagement]

The [Microsoft.SqlServer.Management.AlwaysEncrypted.AlwaysEncryptedManagement] is static. When the "CustomProviders" property is first accessed, the [System.Data.SqlClient.SqlConnection.RegisterColumnEncryptionKeyStoreProviders() method is called. By doing so, the SqlServer PowerShell module lock any further call to this method to register a custom provider.


```CSharp
using System.Data.SqlClient;

public static class AlwaysEncryptedManagement {
    
    private static Dictionary<string, SqlColumnEncryptionKeyStoreProvider> customProviders;

    public static Dictionary<string, SqlColumnEncryptionKeyStoreProvider> CustomProviders
    {
	get
	{
		if (customProviders == null)
		{
			customProviders = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>();
			customProviders["AZURE_KEY_VAULT"] = new SqlColumnEncryptionAzureKeyVaultProvider(GetAccessToken, new string[4]
			{
				"vault.azure.net",
				"vault.azure.cn",
				"vault.usgovcloudapi.net",
				"vault.microsoftazure.de"
			});
			try
			{
				SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders);
			}
			catch (InvalidOperationException)
			{
			}
		}
		return customProviders;
	}
}
```
