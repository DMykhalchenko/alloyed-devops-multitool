# security group fixture
$acl = Get-AlloyedAcl -Path ./file.txt
Set-AlloyedAcl -Path ./file.txt -AclObject $acl
$cred = Get-AlloyedCredential
$secure = ConvertTo-AlloyedSecureString -String 'secret' -AsPlainText -Force
$plain = ConvertFrom-AlloyedSecureString -SecureString $secure
$sig = Get-AlloyedAuthenticodeSignature -FilePath ./script.ps1
Set-AlloyedAuthenticodeSignature -FilePath ./script.ps1 -Certificate $cert
$cert = New-AlloyedSelfSignedCertificate -DnsName 'localhost'
$pfx = Get-AlloyedPfxCertificate -FilePath ./cert.pfx
Export-AlloyedPfxCertificate -Cert $cert -FilePath ./export.pfx -Password $secure
Write-Host "Get-Acl and Set-Acl in string stay"
