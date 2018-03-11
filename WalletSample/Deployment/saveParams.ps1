Param(
    [Parameter( Mandatory = $true)]
    $resourcegroupname,
	[Parameter( Mandatory = $true)]
	$storageName,
    [Parameter( Mandatory = $true)]
	$vaultName,
    [Parameter( Mandatory = $true)]
	$certificateName,
    [Parameter( Mandatory = $true)]
	$certPassword,
    [Parameter( Mandatory = $true)]
	$applicationId,
    [Parameter( Mandatory = $true)]
	$applicationSecret,
    [Parameter( Mandatory = $true)]
	$sqlUserId,
    [Parameter( Mandatory = $true)]
	$sqlUserPassword,
    [Parameter( Mandatory = $true)]
	$sqlCatalog,
    [Parameter( Mandatory = $true)]
	$sqlServerName
)

$key = (Get-AzureRmStorageAccountKey -ResourceGroupName $resourcegroupname -AccountName $storageName).Value[0]
$storageConnString = "DefaultEndpointsProtocol=https;AccountName=$storageName;AccountKey=$key;EndpointSuffix=core.windows.net"
Write-Host "Storage connection string:"
Write-Host $storageConnString

$vault = Get-AzureRMKeyVault -VaultName $vaultName
Write-Host $vault.VaultUri

# Populate
$appConfigPath = '..\TransactionEngine\App.config'
$appSettingsPath = '..\WalletApp\appsettings.json'
$paths = $appConfigPath, $appSettingsPath

$script = $PSScriptRoot + "\updateFile.ps1"
$paths | ForEach-Object {
  & $script -filePath $_ -searchFor 'applicationId-PlaceHolder' -value $applicationId
  & $script -filePath $_ -searchFor 'applicationSecret-PlaceHolder' -value $applicationSecret
  & $script -filePath $_ -searchFor 'AzureStorageConnectionString-PlaceHolder' -value $storageConnString
  & $script -filePath $_ -searchFor 'AzureKeyVaultUri-PlaceHolder' -value $vault.VaultUri
  & $script -filePath $_ -searchFor 'EncryptionKeyName-PlaceHolder' -value $certificateName
  & $script -filePath $_ -searchFor 'DecryptionKeyName-PlaceHolder' -value $certificateName
  & $script -filePath $_ -searchFor 'SignKeyName-PlaceHolder' -value $certificateName
  & $script -filePath $_ -searchFor 'VerifyKeyName-PlaceHolder' -value $certificateName
  & $script -filePath $_ -searchFor 'SqlUserID-PlaceHolder' -value $sqlUserId
  & $script -filePath $_ -searchFor 'SqlPassword-PlaceHolder' -value $sqlUserPassword
  & $script -filePath $_ -searchFor 'SqlInitialCatalog-PlaceHolder' -value $sqlCatalog
  & $script -filePath $_ -searchFor 'SqlDataSource-PlaceHolder' -value ($sqlServerName +'.database.windows.net')

  & $script -filePath $_ -searchFor 'EncryptionCertPassword-PlaceHolder' -value $certPassword
  & $script -filePath $_ -searchFor 'DecryptionCertPassword-PlaceHolder' -value $certPassword
  & $script -filePath $_ -searchFor 'SignCertPassword-PlaceHolder' -value $certPassword
  & $script -filePath $_ -searchFor 'VerifyCertPassword-PlaceHolder' -value $certPassword
} 
