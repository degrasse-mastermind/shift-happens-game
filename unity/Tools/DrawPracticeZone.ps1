using namespace System.Drawing
using namespace System.Drawing.Drawing2D

Add-Type -AssemblyName System.Drawing.Common

$scale = 16
$image = [Bitmap]::new(83 * $scale, 90 * $scale)
$g = [Graphics]::FromImage($image)
$g.SmoothingMode = [SmoothingMode]::AntiAlias
$g.ScaleTransform($scale, $scale)
$g.TranslateTransform(3, 2.5)
function Color($value) { [ColorTranslator]::FromHtml($value) }
function Pen($color, $width) { [Pen]::new((Color $color), $width) }
function Brush($color) { [SolidBrush]::new((Color $color)) }
function Line($pen, $x, $y, $x2, $y2) {
    $g.DrawLine($pen, [single]$x, [single]$y, [single]$x2, [single]$y2)
}

$boards = [GraphicsPath]::new()
$boards.AddArc(0, 0, 56, 56, 180, 90)
$boards.AddLine(28, 0, 83, 0)
$boards.AddLine(83, 0, 83, 85)
$boards.AddLine(83, 85, 28, 85)
$boards.AddArc(0, 29, 56, 56, 90, 90)
$boards.CloseFigure()
$g.FillPath((Brush '#f7fbff'), $boards)
$g.SetClip($boards)
$red = Pen '#b92132' (2/12)
$goalLine = Pen '#b92132' (2/12)
$blue = Pen '#2167aa' 1
Line $blue 75 0 75 85
Line $goalLine 11 0 11 85

foreach ($y in 20.5, 64.5) {
    $g.DrawEllipse($red, 16, [single]($y - 15), 30, 30)
    $g.FillEllipse((Brush '#b92132'), 30, [single]($y - 1), 2, 2)
    foreach ($side in -1, 1) {
        $edge = $y + $side * [Math]::Sqrt(225 - 4)
        foreach ($x in 29, 33) { Line $red $x $edge $x ($edge + $side * 2) }
        foreach ($longSide in -1, 1) {
            $x = 31 + $longSide * 6
            $inside = 31 + $longSide * 2
            $markY = $y + $side * .75
            Line $red $x $markY $inside $markY
            Line $red $inside $markY $inside ($markY + $side * 3)
        }
    }
}

$crease = [GraphicsPath]::new()
$crease.AddLine(11, 38.5, 15.472136, 38.5)
$crease.AddArc(5, 36.5, 12, 12, -41.8103, 83.6206)
$crease.AddLine(15.472136, 46.5, 11, 46.5)
$crease.CloseFigure()
$g.FillPath((Brush '#c1e5f5'), $crease)
$g.DrawPath($red, $crease)

$net = [GraphicsPath]::new()
$net.AddBezier(11, 39.5, 6.1, 39.1, 6.1, 45.9, 11, 45.5)
$net.CloseFigure()
$g.FillPath((Brush '#a3bacb'), $net)
$netState = $g.Save()
$g.SetClip($net, [CombineMode]::Intersect)
$mesh = Pen '#fafdff' .12
foreach ($i in 0..18) {
    $x = 3 + .52 * $i
    Line $mesh $x 38 ($x + 5) 47
    Line $mesh $x 47 ($x + 5) 38
}
$g.Restore($netState)
$g.DrawPath((Pen '#aa1f30' .26), $net)
$posts = Brush '#cf2836'
$g.FillEllipse($posts, 10.79, 39.29, .42, .42)
$g.FillEllipse($posts, 10.79, 45.29, .42, .42)

$g.ResetClip()
$rail = [GraphicsPath]::new()
$rail.AddArc(0, 0, 56, 56, 180, 90)
$rail.AddLine(28, 0, 83, 0)
$rail.StartFigure()
$rail.AddLine(83, 85, 28, 85)
$rail.AddArc(0, 29, 56, 56, 90, 90)
$rail.AddLine(0, 57, 0, 28)
$g.DrawPath((Pen '#273a50' 1.1), $rail)
$g.DrawPath((Pen '#adc1d2' .32), $rail)
$g.Dispose()
$destination = Join-Path $PSScriptRoot '../Assets/Practice/Resources/PracticeZoneIce.png'
$image.Save($destination, [Imaging.ImageFormat]::Png)
$image.Dispose()
