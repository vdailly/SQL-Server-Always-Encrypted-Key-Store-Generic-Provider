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

Always Encrypted feature comes with some builtin key store described below in following table :

<table>
    <thead>
        <tr>
            <th>Provider Name</th>
            <th>Class</th>
            <th>Details</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>MSSQL_CERTIFICATE_STORE</td>
            <td><a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcolumnencryptioncertificatestoreprovider?view=netframework-4.7.2">SqlColumnEncryptionCertificateStoreProvider</a></td>
            <td>Represent the Windows Certificate Store</td>
        </tr>
        <tr>
            <td>MSSQL_CNG_STORE</td>
            <td><a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcolumnencryptioncngprovider?view=netframework-4.7.2">SqlColumnEncryptionCngProvider</a></td>
            <td></td>
        </tr>
        <tr>
            <td>MSSQL_CSP_PROVIDER</td>
            <td><a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcolumnencryptioncspprovider?view=netframework-4.7.2">SqlColumnEncryptionCspProvider</a></td>
            <td></td>
        </tr>
        <tr>
            <td>MSSQL_JAVA_KEYSTORE</td>
            <td><a href="http://static.javadoc.io/com.microsoft.sqlserver/mssql-jdbc/6.1.0.jre7/com/microsoft/sqlserver/jdbc/SQLServerColumnEncryptionJavaKeyStoreProvider.html">SQLServerColumnEncryptionJavaKeyStoreProvider</a></td>
            <td>only available with the JDBC Driver</td>
        </tr>
        <tr>
            <td>AZURE_KEY_VAULT</td>
            <td><a href="https://www.nuget.org/packages/Microsoft.SqlServer.Management.AlwaysEncrypted.AzureKeyVaultProvider/">SqlColumnEncryptionAzureKeyVaultProvider</a></td>
            <td>available for both JDBC/.NET Driver but considered as a custom provider, not registered by default</td>
        </tr>
    </tbody>
</table>


## Interoperability Issue

The following architecture schema describe in details how the SQL Server Always Encrypted feature works. Each arrow is described below in order.

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
    
    3. The client does not have any knowledge of the MSSQL_Certificate_Store. It cannot access the key to decrypt values. Whatever you provide in your connectionstring the use of a JAVA_Key_Store, path to the file, and password. example: "jdbc:sqlserver://server:1433;databaseName=CLINIC;user=admin;password=P@ssw0rd";columnEncryptionSetting=Enabled;keyStoreAuthentication=JavaKeyStore;keyStoreLocation=$HOME/CLINIC-CMK.pfx;keyStoreSecret=SecretP@ssw0rd" ). The JavaKeyStore settings are ignored by the driver.


<br />

## Security Concerns

From my opinion there is no real reasons for the CMK metadata to store both the provider and key path. It should be the client responsability to provide the right key store and key path.

If it is intended to ensure that only one kind of client can decrypt the values, then you probably don't really know to who you gave the certificate. 

If an attacker gain access to a cient able to decrypt the database, the attacker can. If worst the attacker gain access to the SQL Server, it will probably be very easy to gain access to a client able to decrypt the database. 

From the last two sentences, using the AzureKeyVault provider seems a bit more secure, because any client (Windows/Linux) may have access to the web, and it would be probably challenging for an attacker to gain access to the key.

<br />

## Issues discovered

The development of this <b>generic</b> provider for interoperability between operating system highlighted some issues in the original source code from Microsoft, and in a less measure in the Oracle JRE/JDK.

These issues are detailed in following documents:
- Microsoft : [PS Module unable to retrieve a registered custom provider](Issue1.md)
- Microsoft : [Already registered custom provider](Issue2.md)
- Microsoft / Oracle JDK : [Windows Server 2016 certificate are not compatible with the current publicly available JRE/JDK](1-CreateKey.md)

## Solution

Using the provided documentation, its possible to create a generic key store wrapping an underlying real keystore. This solution provide interoperability for clients (Windows/Unix), and do not expose any hint about the path to the key.

This solution provide in order :
1. The [setup of the SQL database table](0-SetupSQL.md).

2. The [generation of the certificate](1-CreateKey.md) used to encrypted columns and deployed to both the JDBC client (as .pfx file for the MSSQL_JAVA_KEYSTORE provider) and the .NET client (as .pfx imported into the Windows Certificate store for the MSSQL_CERTIFICATE_STORE provider). The certificate generation has currently some restrictions detailed in the aformentioned document.

3. the [patched Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll](bin\Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll) (ensure to bypass strong name verification, use at your own risk) to bypass [issue](Issue1.md) encountered.

4. [Extended Always Encrypted cmdlets](bin\SQLServerAlwaysEncrypted.dll) that allow to bypass [issue](Issue2.md) encountered. These cmdlets are extensions for the Always Encrypted Microsoft cmdlets included in the SqlServer PowerShell module.

5. a SQLColumEncryptionGenericKeyStoreProvider class implementation for both JDBC Driver and the .NET Driver. This <b>generic</b> provider is [compiled in DLL](bin\SQLServerAlwaysEncrypted.dll) for usage in the PowerShell configuration of the SQL Server Always Encrypted feature.

6. The [complete documentation](2-CreateGenericCMK-CEK.md) to setup the environment (computer wide, powershell-session wide) and all steps to create, register and use the <b>generic</b> provider, generate the CMK, the CEK and encrypt columns of the table in the database.

7. samples to read encrypted data with both .NET Driver and JDBC Driver :
    - [Sample .NET Driver](3-.NETClient.md)
    - [Sample JDBC Driver](4-JDBCClient.md)

<br />

## Results

- The generic provider CMK (Generic Key Store provider, no meaningfull Key Path) :

![Generic CMK](assets/ssms_cmk_details.png)

<br />

## Known Issues

### SQL Server Management Studio cannot decrypt columns when using the generic provider

SQL Server Management Studio cannot decrypt columns when setting "Column Encryption Setting=enabled". This is because when ssms.exe process start, the generic provider is not registered/available into its process's memory.

![connectionstring](assets/smss_settings.png)

Using the following script on a table with encrypted columns, the following error happens :
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
```
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt column 'SSN'.<br />
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt a column encryption key. Invalid key store provider name: 'GENERIC'...
```

#### Solution 1
Accessing the memory of the process to register the generic provider is probably not an easy task.

#### Solution 2
Another option may relie on the IL code modification to patch any Microsoft assembly loaded by the ssms.exe process :
- discover an assembly loaded by ssms.exe process (preference for a dedicated assembly for the process, not a shared one).
- include a static constructor on any class of this assembly.
- in the static constructor, dynamically load the assembly of the generic provider ([the provided dll](bin/SQLServerAlwaysEncrypted.dll)).
- create an instance of the real provider MSSQL_CERTIFICATE_STORE (SqlColumnEncryptionCertificateStoreProvider).
- create an instance of the generic provider with the wrapped instance of MSSQL_CERTIFICATE_STORE and path to the key of this wrapped provider.
- create a dictionnay of custom providers, adding the generic provider to this dictionnary.
- Register this generic provider in the custom provider, calling SqlConnection.RegisterColumnEncryptionKeyStoreProviders() method.
- Remove Strong Name verification for the updated assembly.


## References Documentation

- [Always Encrypted (Database Engine)](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-2017)
- [Using Always Encrypted with the JDBC driver](https://docs.microsoft.com/en-us/sql/connect/jdbc/using-always-encrypted-with-the-jdbc-driver?view=sql-server-2017)

## Licence

No licence, used to document and report. Use at your own risk.