# Certificate Key Generation for SQL Always Encrypted generic provider

This certificate will be used by the .NET Driver and JDBC Driver using different Key Store.

Points of the script:
- FriendlyName is mandatory for the JavaKeyStore
- Exportable is mandatory: you must distribute this certificate to all clients requiring access to encrypted columns.
- Set the password for your exported certificate.

```PowerShell
# Create a new self signed certificate
$cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My `
            -KeyAlgorithm RSA  `
            -KeyDescription 'SQL Server Always Encrypted CLINIC CMK' `
            -KeyExportPolicy Exportable `
            -KeyLength 4096 `
            -KeySpec KeyExchange `
            -KeyUsage DataEncipherment `
            -KeyUsageProperty All  `
            -NotAfter ([DateTime]::now.AddYears(20)) `
            -NotBefore $([DateTime]::Now.AddDays(-1)) `
            -Subject 'CLINIC_CMK_GENERIC' `
            -Type DocumentEncryptionCert `
            -Provider 'Microsoft Strong Cryptographic Provider' `
            -FriendlyName 'CLINIC_CMK_GENERIC'


#export the certificate in a file
$pwd = ConvertTo-SecureString  "P@ssw0rd" -AsPlainText -Force
Export-PfxCertificate -Cert Cert:\CurrentUser\my\$($cert.Thumbprint) -FilePath "C:\Temp\CLINIC_CMK_GENERIC.pfx" -Password $pwd

```