param([string]$Deck, [string]$OutDir)
# Render every slide of a .pptx to PNG with PowerPoint itself (no LibreOffice on this machine).
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null
Get-ChildItem $OutDir -Filter 'slide-*.png' | Remove-Item -Force
$app = New-Object -ComObject PowerPoint.Application
try {
  $pres = $app.Presentations.Open((Resolve-Path $Deck).Path, $true, $false, $false)  # ReadOnly, Untitled=false, WithWindow=false
  $n = $pres.Slides.Count
  foreach ($s in $pres.Slides) {
    $file = Join-Path $OutDir ("slide-{0:D2}.png" -f $s.SlideIndex)
    $s.Export($file, 'PNG', 1600, 900)
  }
  $pres.Close()
  Write-Output "Exported $n slides to $OutDir"
} finally {
  $app.Quit()
  [System.Runtime.InteropServices.Marshal]::ReleaseComObject($app) | Out-Null
}
