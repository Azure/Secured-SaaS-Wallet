# This script will configure Always Encrypted feature on the selected SQL.
# The key will be stored in Azure Key Vault, and it will encrypt the selected column.


# The Azure tenant id (under active directory properties -> directory Id)
$tenantId  = '[Azure tenant id]' 
# The Azure Service principal application id
$applicationId= '[Azure application id]'
# The  Azure Service principal secret
$applicationSecret = "[Azure application key]"

$resourceGroup = '[The resource group name]'
$azureLocation = '[The resource location]'
$akvName = '[The vault name]'
$akvKeyName = "CMKAuto"
$columnNameToEncrypt = "dbo.accounts.PrivateKey"

$serverName = "[The sql server name]"
$databaseName = "[The database name]"
$sqlUser = "[The sql user]"
$sqlPassword = "[The sql password]"
# Login to Azure with service principal
$SecurePassword = $applicationSecret | ConvertTo-SecureString -AsPlainText -Force
$cred = new-object -typename System.Management.Automation.PSCredential -argumentlist $applicationId, $SecurePassword
Add-AzureRmAccount -Credential $cred -Tenant $tenantId -ServicePrincipal

# Creates a new resource group - skip, if you desire group already exists.
#New-AzureRmResourceGroup –Name $resourceGroup –Location $azureLocation 

# Creates a new key vault - skip if your vault already exists.
#New-AzureRmKeyVault -VaultName $akvName -ResourceGroupName $resourceGroup -Location $azureLocation 

# Sets the key permissions
Set-AzureRmKeyVaultAccessPolicy -VaultName $akvName -ResourceGroupName $resourceGroup -PermissionsToKeys get, create, delete, list, update, import, backup, restore, wrapKey,unwrapKey, sign, verify -ServicePrincipalName $applicationId
$akvKey = Add-AzureKeyVaultKey -VaultName $akvName -Name $akvKeyName -Destination "Software"

# Import the SqlServer module.
Import-Module "SqlServer"

# Connect to your database (Azure SQL database).
$connStr = "Server = " + $serverName + "; Database = " + $databaseName +"; uid="+$sqlUser +";pwd=+"$sqlPassword 
$connection = New-Object Microsoft.SqlServer.Management.Common.ServerConnection
$connection.ConnectionString = $connStr
$connection.Connect()
$server = New-Object Microsoft.SqlServer.Management.Smo.Server($connection)
$database = $server.Databases[$databaseName] 

# Create a SqlColumnMasterKeySettings object for your column master key. 
$cmkSettings = New-SqlAzureKeyVaultColumnMasterKeySettings -KeyURL $akvKey.ID

# Create column master key metadata in the database.
$cmkName = "CMK"
New-SqlColumnMasterKey -Name $cmkName -InputObject $database -ColumnMasterKeySettings $cmkSettings

# Authenticate to Azure
Add-SqlAzureAuthenticationContext -Interactive

# Generate a column encryption key, encrypt it with the column master key and create column encryption key metadata in the database. 
$cekName = "CEK"
New-SqlColumnEncryptionKey -Name $cekName -InputObject $database -ColumnMasterKey $cmkName


# Encrypt the selected columns (or re-encrypt, if they are already encrypted using keys/encrypt types, different than the specified keys/types.
$ces = @()
$ces += New-SqlColumnEncryptionSettings -ColumnName $columnNameToEncrypt -EncryptionType "Deterministic" -EncryptionKey $cekName
Set-SqlColumnEncryption -InputObject $database -ColumnEncryptionSettings $ces -LogFileDirectory .