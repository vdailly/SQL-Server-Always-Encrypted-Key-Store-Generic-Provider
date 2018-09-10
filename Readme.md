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
    
    3. <span style="color:red;">The client does not have any knowledge of the MSSQL_Certificate_Store. It cannot access the key to decrypt values. Whatever you provide in your connectionstring the use of a JAVA_Key_Store, path to the file, and password</span> (exemple: jdbc:sqlserver://server:1433;databaseName=CLINIC;user=admin;password=P@ssw0rd";columnEncryptionSetting=Enabled;keyStoreAuthentication=JavaKeyStorePassword;keyStoreLocation=$HOME/CLINIC-CMK.pfx;keyStoreSecret=SecretP@ssw0rd");)

<br />
<br />
<br />

## Security Concerns

From my opinion there is no real reasons for the CMK metadata to store both the provider and key path. It should be the client responsability to provide the right key store and key path.

If it is intended to ensure that only one kind of client can decrypt the values, then you probably don't really know to who you gave the certificate. 

If an attacker gain access to a cient able to decrypt the database, the attacker can. If worst the attacker gain access to the SQL Server, it will probably be very easy to gain access to a client able to decrypt the database. 

From the last two sentences, using the AzureKeyVault provider seems a bit more secure, because any client (Windows/Linux) may have access to the web, and it would be probably challenging for an attacker to gain access to the key.

<br />

## Solution

Using the provided documentation, its possible to create a generic key store wrapping an underlying real keystore. This solution provide interoperability for clients (Windows/Unix), and do not expose any hint about the path to the CMK.

This solution highlight 2 issues regarding usage of a custom/generic provider. These issues are detailed in:
- [PS Module unable to retrieve a registered custom provider](Issue1.md)
- [Already registered custom provider](Issue2.md)


This solution provide in order:
- [Setup SQL sample database/table](0-SetupSQL.md)
- a SQLColumEncryptionGenericKeyStoreProvider for both JDBC Driver and [.NET Driver](bin\SQLServerAlwaysEncrypted.dll)
- [Extended Always Encrypted cmdlets](bin\SQLServerAlwaysEncrypted.dll) (for the PowerShell SqlServer module) to bypass [issue](Issue2.md) encountered.
- the [patched Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll](bin\Microsoft.SqlServer.Management.AlwaysEncrypted.Management.dll) (ensure to bypass strong name verification, use at your own risk) to bypass [issue](Issue1.md) encountered.
- sample to access encrypted data with both .NET Driver and JDBC Driver

<br />

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

<br />

## Known Issues

- SQL Server Management Studio cannot decrypt columns when setting "Column Encryption Setting=enabled". Or we should access the .NET assemblies loaded by the 'smss' process and register the generic provider into the SqlConnection class loaded by the process. I even don't know if this can be done (easily I mean).

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
<blockquote style="color:red;">
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt column 'SSN'.<br />
Msg 0, Level 11, State 0, Line 0<br />
Failed to decrypt a column encryption key. Invalid key store provider name: 'GENERIC'...
```
</blockquote>


## Licence

No licence, used to document and report. Use at your own risk.