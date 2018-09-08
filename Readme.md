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

![architecture](assets/AlwaysEncryptedMetadata.png) "Always Encrypted Architecture"

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

## Licence

No licence, used to document and report. Use at your own risk.