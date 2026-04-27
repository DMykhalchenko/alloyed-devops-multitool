# utility group fixture — commands in executable positions only
$json = ConvertTo-Json -InputObject @{ Name = 'test' }
$parsed = ConvertFrom-Json -InputObject "ConvertTo-Json literal should stay"
$xml = ConvertTo-Xml -InputObject @{ Name = 'test' } -As String
$sorted = @(3, 1, 2) | Sort-Object
$measured = @(1, 2, 3) | Measure-Object # Sort-Object in comment should stay
$grouped = @('a', 'b', 'a') | Group-Object
$match = Select-String -Pattern 'test' -InputObject 'this is a test'
$n = Get-Random -Minimum 1 -Maximum 100
