param()
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$source = Join-Path $root 'Invoice-Portal-Executive-Overview.pptx'
$pdf = Join-Path $root 'Invoice-Portal-Executive-Overview.pdf'
$images = Join-Path $root 'rendered'
if (-not (Test-Path -LiteralPath $source)) { throw 'Build the presentation first.' }
New-Item -ItemType Directory -Path $images -Force | Out-Null
$app = $null
$deck = $null
try {
    # Only this automation-created presentation is closed. Never terminate other Office processes.
    $app = New-Object -ComObject PowerPoint.Application
    $deck = $app.Presentations.Open($source, -1, 0, 0)
    if ($deck.Slides.Count -ne 20) { throw "Unexpected slide count: $($deck.Slides.Count)" }
    $issues = @()
    for ($i = 1; $i -le $deck.Slides.Count; $i++) {
        $slide = $deck.Slides.Item($i)
        foreach ($shape in $slide.Shapes) {
            if ($shape.HasTextFrame -eq -1 -and $shape.TextFrame.HasText -eq -1) {
                $range = $shape.TextFrame2.TextRange
                $height = $shape.Height - $shape.TextFrame2.MarginTop - $shape.TextFrame2.MarginBottom
                $width = $shape.Width - $shape.TextFrame2.MarginLeft - $shape.TextFrame2.MarginRight
                if ($range.BoundHeight -gt ($height + 3) -or $range.BoundWidth -gt ($width + 3)) {
                    $issues += "Slide ${i}: $($range.Text) [text $($range.BoundWidth)x$($range.BoundHeight), box ${width}x${height}]"
                }
            }
        }
        $slide.Export((Join-Path $images ('slide-{0:D2}.png' -f $i)), 'PNG', 1600, 900)
    }
    # Export with the same desktop PowerPoint engine that opened the PPTX.
    $deck.SaveAs($pdf, 32)
    if ($issues.Count -gt 0) { throw ($issues -join "`n") }
    Add-Type -AssemblyName System.Drawing
    $sheet = New-Object System.Drawing.Bitmap(1600, 1125)
    $graphics = [System.Drawing.Graphics]::FromImage($sheet)
    try {
        $graphics.Clear([System.Drawing.Color]::White)
        for ($i = 1; $i -le 20; $i++) {
            $image = [System.Drawing.Image]::FromFile((Join-Path $images ('slide-{0:D2}.png' -f $i)))
            try {
                $x = (($i - 1) % 4) * 400
                $y = [Math]::Floor(($i - 1) / 4) * 225
                $graphics.DrawImage($image, [int]$x, [int]$y, 400, 225)
            } finally { $image.Dispose() }
        }
        $sheet.Save((Join-Path $images 'contact-sheet.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $sheet.Dispose() }
    @{
        renderer = 'Desktop Microsoft PowerPoint'
        slides = $deck.Slides.Count
        textOverflowIssues = $issues.Count
        pptxSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        renderedAt = (Get-Date).ToUniversalTime().ToString('o')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'render-validation.json') -Encoding UTF8
    Write-Output "PASS: Desktop PowerPoint opened 20 slides; exported PDF and 20 PNGs; no measured text overflow."
} finally {
    if ($null -ne $deck) { $deck.Close(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($deck) }
    if ($null -ne $app) {
        if ($app.Presentations.Count -eq 0) { $app.Quit() }
        [void][Runtime.InteropServices.Marshal]::ReleaseComObject($app)
    }
}