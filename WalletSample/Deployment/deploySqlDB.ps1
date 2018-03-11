# This script creates a SQL server and database within the specified RG 

Param(
    [Parameter( Mandatory = $true)]
    $resourcegroupname,
	[Parameter( Mandatory = $true)]
	$location,
	[Parameter( Mandatory = $true)]
	$adminlogin,
	[Parameter( Mandatory = $true)]
	$password,
    [Parameter( Mandatory = $true)]
	$servername,
    [Parameter( Mandatory = $true)]
	$databasename
)

# The ip address range that you want to allow to access your server
$startip = "0.0.0.0"
$endip = "0.0.0.0"

# Create a server with a system wide unique server name
$server = New-AzureRmSqlServer -ResourceGroupName $resourcegroupname `
    -ServerName $servername `
    -Location $location `
    -SqlAdministratorCredentials $(New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $adminlogin, $(ConvertTo-SecureString -String $password -AsPlainText -Force))

# Create a server firewall rule that allows access from the specified IP range
$serverfirewallrule = New-AzureRmSqlServerFirewallRule -ResourceGroupName $resourcegroupname `
    -ServerName $servername `
    -FirewallRuleName "AllowedIPs" -StartIpAddress $startip -EndIpAddress $endip

# Create a blank database with an S0 performance level
$database = New-AzureRmSqlDatabase  -ResourceGroupName $resourcegroupname `
    -ServerName $servername `
    -DatabaseName $databasename `
    -RequestedServiceObjectiveName "S0"
