# security group fixture
$acl = Get-Acl -Path ./file.txt
Set-Acl -Path ./file.txt -AclObject $acl
$cred = Get-Credential
$secure = ConvertTo-SecureString -String 'secret' -AsPlainText -Force
$plain = ConvertFrom-SecureString -SecureString $secure
$sig = Get-AuthenticodeSignature -FilePath ./script.ps1
Set-AuthenticodeSignature -FilePath ./script.ps1 -Certificate $cert
$cert = New-SelfSignedCertificate -DnsName 'localhost'
$pfx = Get-PfxCertificate -FilePath ./cert.pfx
Export-PfxCertificate -Cert $cert -FilePath ./export.pfx -Password $secure
Write-Host "Get-Acl and Set-Acl in string stay"
