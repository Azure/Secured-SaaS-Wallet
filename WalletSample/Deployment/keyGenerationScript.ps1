# This script logs in to Azure, creates certificate locally, 
# uploads it to Azure KeyVault as a secret

# Parameters

# The Azure tenant id (under active directory properties -> directory Id)
$tenantId  = '[Azure tenant id]' 
# The Azure Service principal application id
$applicationId= '[Azure application id]'
# The  Azure Service principal secret
$applicationSecret = "[Azure application key]"

# The Azure KeyVault Name
$keyvaultName = '[The vault name]'
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
$cert = New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname $dnsName
$pwd = ConvertTo-SecureString -String $plainpass -Force -AsPlainText
$path = 'cert:\localMachine\my\' + $cert.thumbprint 
Export-PfxCertificate -cert $path -FilePath $pfxFilePath -Password $pwd

# Store the certificate in the AzureKeyVault
$flag = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
$collection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
$collection.Import($pfxFilePath, $plainpass, $flag)
$pkcs12ContentType = [System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12
$clearBytes = $collection.Export($pkcs12ContentType)
$fileContentEncoded = [System.Convert]::ToBase64String($clearBytes)
$secret = ConvertTo-SecureString -String $fileContentEncoded -Force -AsPlainText
$secretContentType = 'application/x-pkcs12'
Set-AzureRmKeyVaultAccessPolicy -VaultName $keyvaultName -ServicePrincipalName $applicationId -PermissionsToSecrets set,delete,get,list
Set-AzureKeyVaultSecret -VaultName $keyvaultName -Name $secretName -SecretValue $Secret -ContentType $secretContentType

# Delete local Certificate 
Remove-Item -path $pfxFilePath
