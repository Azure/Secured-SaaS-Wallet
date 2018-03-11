Param ($filePath, $searchFor, $value)
(get-content $filePath) | foreach-object {$_ -replace $searchFor, $value} | set-content $filePath
