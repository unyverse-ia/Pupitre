# Signature Authenticode de dist\Pupitre.exe, sans outil à installer (Set-AuthenticodeSignature de PowerShell).
#
#   .\sign.ps1 -Thumbprint <empreinte>            certificat de signature de code du magasin personnel (Cert:\CurrentUser\My)
#   .\sign.ps1 -Pfx chemin.pfx [-PfxPassword ...]  certificat fourni en fichier PFX
#   .\sign.ps1 -MakeTestCert                       crée un certificat auto-signé « Pupitre (test interne) » et exporte dist\Pupitre-test.cer
#   -NoTimestamp : pas d'horodatage (sinon http://timestamp.digicert.com, nécessite une connexion au moment de la signature)
#
# Un certificat auto-signé n'est reconnu que sur les postes où dist\Pupitre-test.cer a été importé dans
# « Autorités de certification racines de confiance » et « Éditeurs approuvés » (certutil ou stratégie de groupe).
# Smart App Control et SmartScreen n'acceptent sans avertissement qu'un certificat délivré par une autorité reconnue.
param(
    [string]$Thumbprint = $env:PUPITRE_SIGN_THUMBPRINT,
    [string]$Pfx,
    [string]$PfxPassword,
    [string]$File = (Join-Path $PSScriptRoot "dist\Pupitre.exe"),
    [switch]$NoTimestamp,
    [switch]$MakeTestCert
)
$ErrorActionPreference = "Stop"

if ($MakeTestCert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Pupitre (test interne)" -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(3) -KeyExportPolicy Exportable
    $cer = Join-Path $PSScriptRoot "dist\Pupitre-test.cer"
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
    "Certificat de test créé. Empreinte : " + $cert.Thumbprint
    "Certificat public exporté : " + $cer
    "Sur chaque poste utilisateur (invite administrateur) :"
    "  certutil -addstore Root `"$cer`"   et   certutil -addstore TrustedPublisher `"$cer`""
    "Puis signer :  .\sign.ps1 -Thumbprint " + $cert.Thumbprint
    exit 0
}

if (-not (Test-Path $File)) { throw "Fichier introuvable : $File" }
if ($Pfx) {
    $cert = if ($PfxPassword) { New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Pfx, $PfxPassword) } else { Get-PfxCertificate -FilePath $Pfx }
} elseif ($Thumbprint) {
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $Thumbprint } | Select-Object -First 1
    if (-not $cert) { $cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $Thumbprint } | Select-Object -First 1 }
    if (-not $cert) { throw "Aucun certificat avec l'empreinte $Thumbprint dans les magasins personnels." }
} else {
    throw "Indiquez -Thumbprint (ou la variable d'environnement PUPITRE_SIGN_THUMBPRINT), -Pfx, ou -MakeTestCert."
}

$params = @{ FilePath = $File; Certificate = $cert; HashAlgorithm = "SHA256" }
if (-not $NoTimestamp) { $params.TimestampServer = "http://timestamp.digicert.com" }
$result = Set-AuthenticodeSignature @params
"Signature : " + $result.Status + " — " + $result.StatusMessage
$check = Get-AuthenticodeSignature -FilePath $File
"Signataire : " + $check.SignerCertificate.Subject + " (expire le " + $check.SignerCertificate.NotAfter.ToString("dd/MM/yyyy") + ")"
if ($check.TimeStamperCertificate) { "Horodatage : " + $check.TimeStamperCertificate.Subject }
"État de confiance sur ce poste : " + $check.Status
