# This script creates a resource group and deploys into it an Azure KeyVault, 
# AzureStorage, Service Fabric cluster and a SQL server/database.
# Next, it populates the KeyVault with a certificate stored as a secret 
# (pfx - which contains public and private key)

#### Run this script with Admin Privileges #####

# Parameters

# The following are values which you should collect from the Azure portal 
# -- Start --
# The Azure tenant id (under active directory properties -> directory Id)
$tenantId  = '[Azure tenant id]'
# The Azure service principal's object id
$objectId = '[Azure Service principal object id]'
# The Azure Service principal application id
$applicationId= '[Azure application id]'
# The  Azure Service principal secret
$applicationSecret = "[Azure application key]"
# The subscription id, where the resources will be created
$subscriptionId = "Subscription Id"
# -- End --

## The following are values which you decide
## -- Start --
# The Azure resource group name that will hold all the resources
$resourceGroupName = '[The resource group name]'
# The Azure resources geo location
$resourcesLocation = '[The resources location]'
# -- End --

# -------The following parameters are already set, but can be changed if wanted ----
# The Azure KeyVault Name
$keyvaultName = "walletkv$(Get-Random)"
# The Azure KeyVault secret name
$secretName = 'myCert'

# Queue
$storageAccountName = "walletstorage$(Get-Random)"
$queueName = "queue$(Get-Random)"

# Service Fabric
$sfClusterName = "walletsf$(Get-Random)"

# Hard coded values, which can be changed if decided
# Service Fabric
$clustersize = 5
$adminuser = 'nimda13'
$adminpwdPlain = "Password#1234"
$adminpwd= $adminpwdPlain | ConvertTo-SecureString -AsPlainText -Force 

$certpwdPlain = "Password#1234"
$certpwd= $certpwdPlain | ConvertTo-SecureString -AsPlainText -Force
$certfolder="c:\saaswalletcertificates\"

$subname= "$sfClusterName.$resourcesLocation.cloudapp.azure.com"
$vmsku = "Standard_D2_v2"

# Certificate configuration
# The temporary pfx location
$pfxFilePath = 'c:\temp\certificate.pfx'
# Temporary password, for installing and exporting the certificate
$plainpass = '123456' 
# The certificate DNS name
$dnsName = 'testcert.contoso.com'

# SQL DB
$sqlAdminUsername = 'nimda12'
$sqlAdminPassword = 'adminPass#12!word'
$sqlServerName = "server-$(Get-Random)"
$sqlDbName = "walletDatabase"
# SCRIPT START

# Login to Azure with service principal
$SecurePassword = $applicationSecret | ConvertTo-SecureString -AsPlainText -Force
$cred = new-object -typename System.Management.Automation.PSCredential -argumentlist $applicationId, $SecurePassword
Add-AzureRmAccount -Credential $cred -Tenant $tenantId -ServicePrincipal
Select-AzureRmSubscription -SubscriptionId $subscriptionId

# Create Azure Resource group
New-AzureRmResourceGroup -Name $resourceGroupName -Location $resourcesLocation

# Creates the certificate
$cert = New-SelfSignedCertificate -certstorelocation cert:\currentuser\my -dnsname $dnsName
$pwd = ConvertTo-SecureString -String $plainpass -Force -AsPlainText
$path = 'cert:\currentUser\my\' + $cert.thumbprint 
Export-PfxCertificate -cert $path -FilePath $pfxFilePath -Password $pwd

# Store the certificate in the AzureKeyVault
$flag = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
$collection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
$collection.Import($pfxFilePath, $plainpass, $flag)
$pkcs12ContentType = [System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12
$clearBytes = $collection.Export($pkcs12ContentType, $plainpass)
$fileContentEncoded = [System.Convert]::ToBase64String($clearBytes)
$secret = ConvertTo-SecureString -String $fileContentEncoded -Force -AsPlainText
$secretContentType = 'application/x-pkcs12'

# Create the Key vault
# Note: We are using the KeyVault related methods BEFORE the Service Fabric methods. due to the following known issue:
# https://github.com/Azure/azure-powershell/issues/5636
New-AzureRmKeyVault -VaultName $keyvaultName -ResourceGroupName $resourceGroupName -Location $resourcesLocation
Set-AzureRmKeyVaultAccessPolicy -VaultName $keyvaultName -ObjectId $objectId -PermissionsToSecrets Get,Set,List
Set-AzureKeyVaultSecret -VaultName $keyvaultName -Name $secretName -SecretValue $secret -ContentType $secretContentType

# Delete local Certificate 
Remove-Item -path $pfxFilePath

# Deploy SQL server and configure it
$script = $PSScriptRoot + "\deploySqlDB.ps1"
& $script -resourcegroupname $resourceGroupName -location $resourcesLocation -adminlogin $sqlAdminUsername -password $sqlAdminPassword -servername $sqlServerName -databasename $sqlDbName

# make sure the path exists
New-Item -ItemType Directory -Force -Path $certfolder

# Create the Service Fabric cluster.
New-AzureRmServiceFabricCluster -Name $sfClusterName -ResourceGroupName $resourceGroupName -Location $resourcesLocation `
-ClusterSize $clustersize -VmPassword $adminpwd -CertificateSubjectName $subname `
-CertificateOutputFolder $certfolder -CertificatePassword $certpwd `
-OS WindowsServer2016Datacenter

# Create Azure storage (queue)
$storageAccount = New-AzureRmStorageAccount -ResourceGroupName $resourceGroupName `
  -Name $storageAccountName `
  -Location $resourcesLocation `
  -SkuName Standard_LRS

$ctx = $storageAccount.Context
$queue = New-AzureStorageQueue -Name $queueName -Context $ctx

# Save all generate values to config files
$script = $PSScriptRoot + "\saveParams.ps1"
& $script -resourcegroupname $resourceGroupName -storageName $storageAccountName -vaultName $keyvaultName -certificateName $secretName -certPassword $plainpass -applicationId $applicationId -applicationSecret $applicationSecret -sqlUserId $sqlAdminUsername -sqlUserPassword $sqlAdminPassword -sqlCatalog $sqlDbName -sqlServerName $sqlServerName
