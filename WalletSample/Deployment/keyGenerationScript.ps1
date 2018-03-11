# This script logs in to Azure, creates certificate locally, 
# uploads it to Azure KeyVault as a secret

 function Upload-Certificate {
  Param ([string] $certFilePath, [string] $filePassword, [string] $keyvaultName, [string] $applicationId, [string] $secretName)
 
  $flag = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
  $collection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
  $collection.Import($certFilePath, $filePassword, $flag)
  $pkcs12ContentType = [System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12
  $clearBytes = $collection.Export($pkcs12ContentType)
  $fileContentEncoded = [System.Convert]::ToBase64String($clearBytes)
  $secret = ConvertTo-SecureString -String $fileContentEncoded -Force -AsPlainText
  $secretContentType = 'application/x-pkcs12'
  Set-AzureRmKeyVaultAccessPolicy -VaultName $keyvaultName -ServicePrincipalName $applicationId -PermissionsToSecrets set,delete,get,list
  Set-AzureKeyVaultSecret -VaultName $keyvaultName -Name $secretName -SecretValue $Secret -ContentType $secretContentType
 }

# Parameters

# The Azure tenant id (under active directory properties -> directory Id)
$tenantId  = '[Azure tenant id]' 
# The Azure Service principal application id
$applicationId= '[Azure application id]'
# The  Azure Service principal secret
$applicationSecret = "[Azure application key]"

# The Azure KeyVault Name
$keyvaultName = '[The vault name]'

# The Azure Keyvault who holds the public keys only
$globalKeyvaultName = '[The vault name]'

# The Azure KeyVault secret name
$secretName = '[The keyvault secret name example: encryptionCert]'

#certificate configuration
# The temporary pfx location
$certTempDirectory =  '[Add here the cert temp location, ex: c:\temp]'
# Temporary password, for installing and exporting the certificate
$plainpass = '[Cert temp directory, ex: 123456]' 
# The certificate DNS name
$dnsName = '[The certificate dns name, example: testcert.contoso.com]'

# Login to Azure with service principal
$SecurePassword = $applicationSecret | ConvertTo-SecureString -AsPlainText -Force
$cred = new-object -typename System.Management.Automation.PSCredential -argumentlist $applicationId, $SecurePassword
Add-AzureRmAccount -Credential $cred -Tenant $tenantId -ServicePrincipal

# Creates the certificate
$pfxFilePath = $certTempDirectory + $secretName + '.pfx'
$cerFilePath = $certTempDirectory + $secretName + '.cer'
$cert = New-SelfSignedCertificate -certstorelocation cert:\currentuser\my -dnsname $dnsName
$pwd = ConvertTo-SecureString -String $plainpass -Force -AsPlainText
$path = 'cert:\currentUser\my\' + $cert.thumbprint 
# With private portion
Export-PfxCertificate -cert $path -FilePath $pfxFilePath -Password $pwd
# With public portion
Export-Certificate -cert $path -FilePath $cerFilePath

# Store the certificate in the AzureKeyVault
Upload-Certificate -certFilePath $pfxFilePath -filePassword $plainpass -keyvaultName $keyvaultName -applicationId $applicationId -secretName $secretName
# Delete local PFX Certificate file 
Remove-Item -path $pfxFilePath

# Uncomment the following lines in order to upload the public key to a special global key vault:
# Upload-Certificate -certFilePath $cerFilePath -filePassword "" -keyvaultName $globalKeyvaultName -applicationId $applicationId -secretName $secretName
## Delete local CER Certificate file 
# Remove-Item -path $cerFilePath
