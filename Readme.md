## SQL Server Always Encrypted Key Store Generic Provider

This solution provide a workaroud for the SQL Server Always Encrypted feature, for interoperability between clients that do not share any common Key Store Provider. Especially, this is intended for OS interoperability (Windows/Linux) not using Azure (or with no access to Internet).

## Always Encrypted definitions

- [SQL Server Always Encrypted documentation](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-2017)
- [Column Master Key / Column Encryption Key overview](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/overview-of-key-management-for-always-encrypted?view=sql-server-2017)

#### Column Master Key

Colum Master Key (CMK) represent a key or generally a certificate. Clients accessing the SQL Server must have access to both the public and private keys of the certificate.

#### Colum Encryption Key

Column Encryption Key (CEK) represent a key used to encrypt the values stored in a database column. The CEK is encrypted with the CMK.

#### Database Columns

Columns of the database are encrypted with the Column Encryption Key (CEK) using either a Deterministic or Randomized algorithm.

#### Keys/Certificate Store

Always Encrypted feature comes with some builtin key store:
1. MSSQLCertificateStore: Represent the Microsoft Windows Certificate Store
2. CNG ...
3. TMG ...
4. JAVAKEYStore: only available with the JDBC Driver.
5. AZUREKEYVault:


## Interoperability Issue

The following architecture schema describe in details the behaviour.
All the detailed commands and steps are described [link?](http://link)

![architecture](assets/AlwaysEncryptedMetadata.png)

##### Key generation and deployement
 1. You generate a certificate (public/private keys) and you deploy this keys to clients allowed to decrypt columns.

 2. You have a Windows Client (MSSQL_Certificate_Store) and an Unix client ( JavaKeyStore), you provide the certificate to both clients.

    1. You import the certificate (.pfx) in the Windows certificate store on the Windows client.
    2. You store the certificate on the file system as file (.pfx) for the JDBC client.

##### CMK / CEK / Database Columns Encryption
 3. To stick on a real production example, you configure Always Encrypted keys provisioning with role separation as described in https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-always-encrypted-keys-using-powershell?view=sql-server-2017#KeyProvisionWithRoles. All steps are described in this document.

    1. The security administrator with access to the certificate private key generate an encrypted value for the CEK.

    2. The DBA administrator get this encrypted value and generate both CMK (with the metadata: the Key Store Provider and the Key Path).

    3. The security administrator can now encrypt colums.

##### .NET Client Data Access

4. The .NET client connect to the database and attempt to decrypt encrypted values in encrypted columns.

    1. Internally, the .NET Driver call the store procedure [sys.sp_describe_parameter_encryption](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-describe-parameter-encryption-transact-sql?view=sql-server-2017)

    2. [supposed] The .NET Driver read the metadata of the CMK and check if it has access to the provider (MSSQL_Certificate_Store) and key path.

    3. The database return encrypted values.

    4. The .NET Driver can decrypt the encrypted values.

##### JDBC Client Data Access

4. The JDBC client connect to the database and attempt to decrypt encrypted values in encrypted columns.

    1. Internally, the JDBC Driver call the store procedure [sys.sp_describe_parameter_encryption](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-describe-parameter-encryption-transact-sql?view=sql-server-2017)

    2. [supposed] The JDBC Driver read the metadata of the CMK and check if it has access to the provider (MSSQL_Certificate_Store) and key path.
    
    3. <span style="color:red;">The client does not have any knowledge of the MSSQL_Certificate_Store. It cannot access the key to decrypt values. Whatever you provide in your connectionstring the use of a JAVA_Key_Store, path to the file, and password</span> (exemple: jdbc:sqlserver://server:1433;databaseName=CLINIC;user=admin;password=P@ssw0rd";columnEncryptionSetting=Enabled;keyStoreAuthentication=JavaKeyStorePassword;keyStoreLocation=$HOME/CLINIC-CMK.pfx;keyStoreSecret=SecretP@ssw0rd");)



## Security Concerns

From my opinion there is no real reasons for the CMK metadata to store both the provider and key path. It should be the client responsability to provide the right key store and key path.

If it is intended to ensure that only one kind of client can decrypt the values, then you probably don't really know to who you gave the certificate. 

If an attacker gain access to a cient able to decrypt the database, the attacker can. If worst the attacker gain access to the SQL Server, it will probably be very easy to gain access to a client able to decrypt the database. 

From the last two sentences, using the AzureKeyVault provider seems a bit more secure, because any client (Windows/Linux) may have access to the web, and it would be probably challenging for an attacker to gain access to the key.


## Solution

Using the provided documentation, its possible to create a generic key store wrapping an underlying real keystore. This solution provide interoperability for clients (Windows/Unix), and do not expose any hint about the path to the CMK.


This solution provide :
- a SQLColumEncryptionGenericKeyStoreProvider
- some extended Always Encrypted cmdlets (PowerShell SqlServer module)
- a patched .dll (with removal of the strong key assembly, use at your own risk)
- allow loading of unsigned assembly
- easily bypass some restrictions with Reflection.
- some low levels information about the current implementation



## Results

- [a generic CMK]()
- [CEK encrypted with the not so generic CMK]()
- [Database encryption]()
- [.NET client use the underlying MSSQLStoreProvider]()
- [JDBC client use the underlying JAVAKeyStoreProvider]()
- Patch the DLL
- Authorize the patched DLL
- Import SQLServer module
- Import Extended Always Encrypted cmdlets ()
- Create the underlying provider you want to use.
- Create the generic provider
- Register the generic provider (Microsoft issue 1)

Initial code from Microsoft
```CSharp
private static SqlColumnEncryptionKeyStoreProvider GetProvider(string providerName)
{
	switch (providerName)
	{
	case "MSSQL_CERTIFICATE_STORE":
		return new SqlColumnEncryptionCertificateStoreProvider();
	case "MSSQL_CSP_PROVIDER":
		return new SqlColumnEncryptionCspProvider();
	case "MSSQL_CNG_STORE":
		return new SqlColumnEncryptionCngProvider();
	case "AZURE_KEY_VAULT":
	{
		if (CustomProviders.TryGetValue("AZURE_KEY_VAULT", out SqlColumnEncryptionKeyStoreProvider value))
		{
			return value;
		}
		string message = string.Format(CultureInfo.CurrentUICulture, ManagementResources.AkvProviderNotRegisteredTemplate, "AZURE_KEY_VAULT");
		throw new InvalidOperationException(message);
	}
	default:
		throw new InvalidOperationException(string.Format(CultureInfo.CurrentUICulture, ManagementResources.UnsupportedKeyStoreProviderTemplate, providerName));
	}
}
```

The main issue in this code is the default clause. Only an error is returned whenever you attempt to access the generic provider. So it is impossible to achieve Always Encrypted configuration with PowerShell. It is certainly possible with C# or Java, but all the documentation used PowerShell and daily management tasks seems easier.

The provided patched DLL use the following code. I replaced the IL code to this simple sentence. I was not really able to properly insert IL to only replace the default statement. But it doesn't have any importance, being able to retrieve the generic provider is sufficient.

I used ILSpy + Reflexil to achieve this.

```CSharp
private static SqlColumnEncryptionKeyStoreProvider GetProvider(string providerName)
{
	if (!CustomProviders.TryGetValue(providerName, out SqlColumnEncryptionKeyStoreProvider value))
	{
		goto IL_000f;
	}
	goto IL_000f;
	IL_000f:
	return value;
}
```

|Offset	|OpCode	|Operand|
|-------|-------|-------|
|0	    |call	|System.Collections.Generic.Dictionary`2<System.String,System.Data.SqlClient.SqlColumnEncryptionKeyStoreProvider> Microsoft.SqlServer.Management.AlwaysEncrypted.Management.AlwaysEncryptedManagement::get_CustomProviders()
|5	    |ldarg.0	
|6	    |ldloca.s  |-> (0) (System.Data.SqlClient.SqlColumnEncryptionKeyStoreProvider)
|8	    |callvirt  |System.Boolean System.Collections.Generic.Dictionary`2<System.String,System.Data.SqlClient.SqlColumnEncryptionKeyStoreProvider>::TryGetValue(!0,!1&)
|13	    |ldloc.0	
|14	    |ret	

Allow our patched dll to be loaded. Because the code is now updated, ensure to trust this new dll, by removing the strong name checking. Reflexil do it for us.

If you plan to use directly the provided "System.Management.SQLServer.Management.dll", you have to register this assembly to bypass Strong Name verification. Else the SQLServer Powershell module will not load the dll properly.


##### Command
```Cmd
rem Find the sn tool in your .NET Framewrok SDK (path may be different on your host).
cd "C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools"
rem Check your CLR policy for Bypass Strong Name verification
sn -Pb
rem Enable Bypass Strong Name verification
sn -Pb y
rem Register the DLL for strong name verification skipping
sn -Vr "Microsoft.SqlServer.Management.AlwaysEncrypted.Management,89845DCD8080CC91" AllUsers
rem Check the DLL registration
sn -Vl
```

##### Command Result
```cmd
Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.33440
Copyright (c) Microsoft Corporation.  All rights reserved.

Trusted applications may bypass strong name verification on this machine.

Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.33440
Copyright (c) Microsoft Corporation.  All rights reserved.

Verification entry added for assembly 'Microsoft.SqlServer.Management.AlwaysEncrypted.Management,89845DCD8080CC91'


Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.33440
Copyright (c) Microsoft Corporation.  All rights reserved.

Assembly/Strong Name                  Users
===========================================
Microsoft.SqlServer.Management.AlwaysEncrypted.Management,89845DCD8080CC91 AllUsers
```

## Already registered custom provider

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

The main issue comes from the latest lines. The code check if the private field "_CustomColumnEncryptionKeyStoreProviders" is null. But the SqlServer PowerShell cmdlets calls:
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


3. Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll: [Microsoft.SqlServer.Management.AlwaysEncrypted.Types.AlwaysEncryptedManagement]

The [Microsoft.SqlServer.Management.AlwaysEncrypted.Types.AlwaysEncryptedManager] is static. When the "CustomProviders" property is first accessed, the [System.Data.SqlClient.SqlConnection.RegisterColumnEncryptionKeyStoreProviders() method is called. By doing so, the SqlServer PowerShell module lock any further call to register a custom provider.


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


## Known Issues

- SQL Server Management Studio cannot decrypt columns when setting "Column Encryption Setting=enabled". Or we should access the .NET assemblies loaded by the 'smss' process and register the generic provider into the SqlConnection class loaded by the process. I even don't know if this can be done.

![connectionstring](assets/smss_settings.png)

```sql
SELECT [PatientID]
      ,[SSN]
      ,[FirstName]
      ,[LastName]
      ,[MiddleName]
      ,[StreetAddress]
      ,[City]
      ,[ZipCode]
      ,[State]
      ,[BirthDate]
  FROM [CLINIC].[dbo].[Patients]
```

Output:
<blockquote style="color:red; size: 8;">
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt column 'SSN'.<br />
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt a column encryption key. Invalid key store provider name: 'GENERIC'...
```
</blockquote>


## Licence

No licence, used to document and report. Use at your own risk.