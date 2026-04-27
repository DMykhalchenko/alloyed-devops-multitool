# utility group fixture — commands in executable positions only
$json = ConvertTo-AlloyedJson -InputObject @{ Name = 'test' }
$parsed = ConvertFrom-AlloyedJson -InputObject "ConvertTo-Json literal should stay"
$xml = ConvertTo-AlloyedXml -InputObject @{ Name = 'test' } -As String
$sorted = @(3, 1, 2) | Sort-AlloyedObject
$measured = @(1, 2, 3) | Measure-AlloyedObject # Sort-Object in comment should stay
$grouped = @('a', 'b', 'a') | Group-AlloyedObject
$match = Select-AlloyedString -Pattern 'test' -InputObject 'this is a test'
$n = Get-AlloyedRandom -Minimum 1 -Maximum 100
