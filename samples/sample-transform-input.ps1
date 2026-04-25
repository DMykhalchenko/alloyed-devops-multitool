# Sample script for end-to-end transformation smoke.
$items = Get-ChildItem -Path .
$item = Get-Item -Path .
$exists = Test-Path -Path .

[pscustomobject]@{
    Count = $items.Count
    ItemName = $item.Name
    Exists = $exists
}
