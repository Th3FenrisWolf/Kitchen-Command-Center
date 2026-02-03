$name = Get-Item *.sln
Get-ChildItem "./src/$($name.BaseName).Web/App_Data/CIRepository" -Recurse | Where-Object { $_.name -like "*bizstreamstarter*" } | Rename-Item -NewName { $_.name -replace 'bizstreamstarter', $name.BaseName.ToLower() }
