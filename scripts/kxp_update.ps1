Write-Host "Installing SqlServer Module if not already installed..."
Install-Module -Name SqlServer

Write-Host "`nDisabling CI..."
Invoke-Sqlcmd -Query "UPDATE CMS_SettingsKey SET KeyValue = N'False' WHERE KeyName = N'CMSEnableCI'" -ConnectionString "Data Source=localhost;Initial Catalog=KitchenCommandCenter;User ID=bzs-xbk;Password=bzs-xbk_440/,.;Persist Security Info=False;Connect Timeout=60;Encrypt=False;Current Language=English;"

Write-Host "`nRunning Update..."
dotnet run --project ./src/KitchenCommandCenter.Web/KitchenCommandCenter.Web.csproj --kxp-update

Write-Host "Enabling CI..."
Invoke-Sqlcmd -Query "UPDATE CMS_SettingsKey SET KeyValue = N'True' WHERE KeyName = N'CMSEnableCI'" -ConnectionString "Data Source=localhost;Initial Catalog=KitchenCommandCenter;User ID=bzs-xbk;Password=bzs-xbk_440/,.;Persist Security Info=False;Connect Timeout=60;Encrypt=False;Current Language=English;"

Write-Host "`nStoring CI..."
dotnet run --project ./src/KitchenCommandCenter.Web/KitchenCommandCenter.Web.csproj --no-build --kxp-ci-store
