Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function New-FontBody { param([float]$size=10) try { New-Object System.Drawing.Font("Segoe UI Variable Text",$size,[System.Drawing.FontStyle]::Regular) } catch { New-Object System.Drawing.Font("Segoe UI",$size,[System.Drawing.FontStyle]::Regular) } }
function New-FontDisplay { param([float]$size=16) try { New-Object System.Drawing.Font("Segoe UI Variable Display",$size,[System.Drawing.FontStyle]::Bold) } catch { try { New-Object System.Drawing.Font("Segoe UI Semibold",$size,[System.Drawing.FontStyle]::Regular) } catch { New-Object System.Drawing.Font("Segoe UI",$size,[System.Drawing.FontStyle]::Bold) } } }
function New-FontSemi { param([float]$size=10) try { New-Object System.Drawing.Font("Segoe UI Semibold",$size,[System.Drawing.FontStyle]::Regular) } catch { New-Object System.Drawing.Font("Segoe UI",$size,[System.Drawing.FontStyle]::Bold) } }

$FONT_TEXT      = New-FontBody 10
$FONT_TEXT_SM   = New-FontBody 9
$FONT_BUTTON    = New-FontSemi 10
$FONT_TITLE     = New-FontDisplay 16

$CURRENT_VERSION = "0.0.3"
$GITHUB_RAW_VERSION_URL = "https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/refs/heads/main/version.txt"
$GITHUB_RAW_GUS_URL = "https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/main/GameUserSettings.ini"
$RELEASE_URL = "https://github.com/arsenzaaa/FORTNITE-UTILITY/releases"
$VERSION_DIR = Join-Path $env:LOCALAPPDATA "FortniteUtility"

if (-not (Test-Path $VERSION_DIR)) { New-Item -Path $VERSION_DIR -ItemType Directory -Force | Out-Null }
$VERSION_FILE = Join-Path $VERSION_DIR "version.txt"

function Write-VersionFile { param([string]$time,[string]$ver,[string]$skip) @("time: $time","ver: $ver","skip: $skip") | Out-File -FilePath $VERSION_FILE -Encoding UTF8 -Force }
function Read-VersionFile {
    $r = @{ time=$null; ver=$null; skip=$null }
    if (Test-Path $VERSION_FILE) {
        Get-Content $VERSION_FILE -ErrorAction SilentlyContinue | ForEach-Object {
            if ($_ -match "^\s*time\s*:\s*(.+)$") { $r.time=$Matches[1].Trim() }
            if ($_ -match "^\s*ver\s*:\s*(.+)$")  { $r.ver =$Matches[1].Trim() }
            if ($_ -match "^\s*skip\s*:\s*(.*)$") { $r.skip=$Matches[1].Trim() }
        }
    }
    $r
}
function Get-RemoteVersion { try { ($resp = Invoke-WebRequest -Uri $GITHUB_RAW_VERSION_URL -UseBasicParsing -TimeoutSec 6 -ErrorAction Stop).Content.Split("`n")[0].Trim() } catch { $null } }
function Convert-ToInt { param([object]$v) if ($null -eq $v) { return 0 } $x = if ($v -is [System.Array]) { if ($v.Length -gt 0){$v[0]} else {0} } else {$v}; try { [int]$x } catch { $s=[string]$x; if ($s -match "\d+") { [int]$Matches[0] } else { 0 } } }

function Show-DarkConfirm {
    param([string]$Title,[string]$Text,[string]$YesText,[string]$NoText,[string]$ButtonStyle,[object]$Width,[object]$Height)
    $w = Convert-ToInt $Width; $h = Convert-ToInt $Height
    if ($w -lt 240) { $w = 240 }; if ($h -lt 120) { $h = 120 }
    $dlg = New-Object System.Windows.Forms.Form
    $dlg.Text = $Title; $dlg.FormBorderStyle = "FixedDialog"; $dlg.StartPosition = "CenterParent"
    $dlg.ClientSize = New-Object System.Drawing.Size($w,$h)
    $dlg.MinimizeBox = $false; $dlg.MaximizeBox = $false
    $dlg.BackColor = [System.Drawing.Color]::FromArgb(30,30,30); $dlg.ForeColor = [System.Drawing.Color]::FromArgb(230,230,230)
    $dlg.Font = $FONT_TEXT
    try { $pi=$dlg.GetType().GetProperty("DoubleBuffered",[Reflection.BindingFlags] "Instance,NonPublic"); if($pi){$pi.SetValue($dlg,$true,$null)} } catch {}
    $fg=[System.Drawing.Color]::FromArgb(230,230,230); $accent=[System.Drawing.Color]::FromArgb(60,120,200); $muted=[System.Drawing.Color]::FromArgb(70,70,70)
    $pic=New-Object System.Windows.Forms.PictureBox; $pic.Size=New-Object System.Drawing.Size(32,32); $pic.Location=New-Object System.Drawing.Point(18,18); $pic.SizeMode="StretchImage"
    if ($Title -match "(?i)warning|cache clear") { $pic.Image=[System.Drawing.SystemIcons]::Warning.ToBitmap() }
    elseif ($Title -match "(?i)confirm|skip|question|update") { $pic.Image=[System.Drawing.SystemIcons]::Question.ToBitmap() } else { $pic.Image=[System.Drawing.SystemIcons]::Information.ToBitmap() }
    $dlg.Controls.Add($pic)
    $lbl=New-Object System.Windows.Forms.Label; $lbl.Location=New-Object System.Drawing.Point(60,18); $lbl.Size=New-Object System.Drawing.Size(($w-80), ($h-80))
    $lbl.Text=$Text; $lbl.Font=$FONT_TEXT_SM; $lbl.ForeColor=$fg; $lbl.BackColor=$dlg.BackColor; $lbl.TextAlign="TopLeft"; $dlg.Controls.Add($lbl)
    if ($ButtonStyle -eq "YesNo") {
        $btnYes=New-Object System.Windows.Forms.Button; $btnYes.Text=$YesText; $btnYes.Size=New-Object System.Drawing.Size(110,34)
        $btnYes.Location=New-Object System.Drawing.Point([int]($w/2 - 120), ($h - 55)); $btnYes.BackColor=$accent; $btnYes.ForeColor=[System.Drawing.Color]::White
        $btnYes.FlatStyle='Flat'; try{$btnYes.FlatAppearance.BorderSize=0}catch{}; $btnYes.DialogResult=[System.Windows.Forms.DialogResult]::Yes
        $btnYes.Font=$FONT_BUTTON; $dlg.Controls.Add($btnYes)
        $btnNo=New-Object System.Windows.Forms.Button; $btnNo.Text=$NoText; $btnNo.Size=New-Object System.Drawing.Size(110,34)
        $btnNo.Location=New-Object System.Drawing.Point([int]($w/2 + 10), ($h - 55)); $btnNo.BackColor=$muted; $btnNo.ForeColor=$fg
        $btnNo.FlatStyle='Flat'; try{$btnNo.FlatAppearance.BorderSize=0}catch{}; $btnNo.DialogResult=[System.Windows.Forms.DialogResult]::No
        $btnNo.Font=$FONT_TEXT; $dlg.Controls.Add($btnNo); $dlg.AcceptButton=$btnYes; $dlg.CancelButton=$btnNo
    } else {
        $btnOK=New-Object System.Windows.Forms.Button; $btnOK.Text=$YesText; $btnOK.Size=New-Object System.Drawing.Size(110,34)
        $btnOK.Location=New-Object System.Drawing.Point([int]($w/2-55), ($h - 55)); $btnOK.BackColor=$accent; $btnOK.ForeColor=[System.Drawing.Color]::White
        $btnOK.FlatStyle='Flat'; try{$btnOK.FlatAppearance.BorderSize=0}catch{}; $btnOK.DialogResult=[System.Windows.Forms.DialogResult]::OK
        $btnOK.Font=$FONT_BUTTON; $dlg.AcceptButton=$btnOK; $dlg.Controls.Add($btnOK)
    }
    $dlg.ShowDialog()
}

if (-not (Test-Path $VERSION_FILE)) { Write-VersionFile -time (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") -ver $CURRENT_VERSION -skip "" }
$vf = Read-VersionFile; $LAST_CHECK = $vf.time; $INSTALLED_VER = $vf.ver; $SKIP_VER = $vf.skip; $shouldCheck = $true
if ($args.Length -gt 0 -and $args[0] -eq 'soft' -and $LAST_CHECK) { try { if ([math]::Floor((New-TimeSpan -Start (Get-Date $LAST_CHECK) -End (Get-Date)).TotalMinutes) -le 360) {$shouldCheck=$false} } catch { $shouldCheck=$true } }
$remoteVersion = if ($shouldCheck) { Get-RemoteVersion } else { $null }
if ($remoteVersion) {
    Write-VersionFile -time (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") -ver $INSTALLED_VER -skip $SKIP_VER
    if ($remoteVersion -ne $INSTALLED_VER -and $remoteVersion -ne $SKIP_VER) {
        $r1 = Show-DarkConfirm -Title "Update available" -Text "New version found: $remoteVersion`nOpen releases page?" -YesText "Yes" -NoText "No" -ButtonStyle "YesNo" -Width 420 -Height 150
        if ($r1 -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process $RELEASE_URL }
        else {
            $r2 = Show-DarkConfirm -Title "Skip version?" -Text "Skip version $remoteVersion and don't ask again?" -YesText "Yes" -NoText "No" -ButtonStyle "YesNo" -Width 440 -Height 150
            if ($r2 -eq [System.Windows.Forms.DialogResult]::Yes) { Write-VersionFile -time (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") -ver $INSTALLED_VER -skip $remoteVersion }
            else { Write-VersionFile -time (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") -ver $INSTALLED_VER -skip $SKIP_VER }
        }
    }
}

function Kill-FortniteProcesses { foreach ($n in @("FortniteLauncher","FortniteClient-Win64-Shipping_EAC_EOS","FortniteClient-Win64-Shipping","CrashReportClient")) { try { Get-Process -Name $n -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch {} } }
function Clear-Cache {
    Kill-FortniteProcesses
    $gusPath = Join-Path $env:USERPROFILE "AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"
    $gusDesktop = Join-Path $env:USERPROFILE "Desktop\GameUserSettings.ini"
    if (Test-Path $gusPath) { try { Move-Item $gusPath $gusDesktop -Force -ErrorAction SilentlyContinue } catch {} }
    $gusDir = Split-Path $gusPath -Parent
    if (-not (Test-Path $gusDir)) { New-Item $gusDir -ItemType Directory -Force | Out-Null }
    if (Test-Path $gusDesktop) { try { Move-Item $gusDesktop $gusPath -Force -ErrorAction SilentlyContinue } catch {} }
    foreach ($p in @("$env:USERPROFILE\AppData\Local\D3DSCache\*","$env:USERPROFILE\AppData\Local\NVIDIA\DXCache\*","$env:USERPROFILE\AppData\LocalLow\NVIDIA\PerDriverVersion\DXCache\*")) { try { Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue } catch {} }
    foreach ($d in @("$env:USERPROFILE\AppData\Local\CrashReportClient","$env:USERPROFILE\AppData\Local\EpicOnlineServicesUIHelper","$env:USERPROFILE\AppData\Local\EpicGamesLauncher\Saved\Config\CrashReportClient","$env:USERPROFILE\AppData\Local\EpicGamesLauncher\Saved\Logs")) { if (Test-Path $d) { try { Remove-Item $d -Recurse -Force -ErrorAction SilentlyContinue } catch {} } }
    $saved = "$env:USERPROFILE\AppData\Local\EpicGamesLauncher\Saved"; if (Test-Path $saved) { Get-ChildItem $saved -Directory -Filter "webcache*" -ErrorAction SilentlyContinue | ForEach-Object { try { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue } catch {} } }
    $eac = "$env:USERPROFILE\AppData\Roaming\EasyAntiCheat"; if (Test-Path $eac) { Get-ChildItem $eac -Recurse -Filter "*.log" -ErrorAction SilentlyContinue | ForEach-Object { try { Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue } catch {} } }
    [void](Show-DarkConfirm -Title "Done" -Text "Shader cleanup is complete!`nRestart your computer!" -YesText "OK" -ButtonStyle "OK" -Width 360 -Height 120)
}

function Get-GUSDir { Join-Path $env:USERPROFILE "AppData\Local\FortniteGame\Saved\Config\WindowsClient" }
function Get-GUSPath { Join-Path (Get-GUSDir) "GameUserSettings.ini" }
function Download-GameUserSettings {
    Kill-FortniteProcesses
    $destDir = Get-GUSDir; if (-not (Test-Path $destDir)) { New-Item $destDir -ItemType Directory -Force | Out-Null }
    $tmp = Join-Path $env:TEMP "GameUserSettings.ini"
    try { Invoke-WebRequest -Uri $GITHUB_RAW_GUS_URL -UseBasicParsing -TimeoutSec 12 -OutFile $tmp -ErrorAction Stop } catch { [void](Show-DarkConfirm -Title "Error" -Text "Failed to download GameUserSettings.ini from GitHub." -YesText "OK" -ButtonStyle "OK" -Width 460 -Height 140); return $false }
    try { Move-Item $tmp (Join-Path $destDir "GameUserSettings.ini") -Force -ErrorAction SilentlyContinue; [void](Show-DarkConfirm -Title "Success" -Text "GameUserSettings.ini has been installed." -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 120); return $true }
    catch { [void](Show-DarkConfirm -Title "Error" -Text "Failed to place GameUserSettings.ini." -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 120); return $false }
}

function Write-GUSValues {
    param([string]$rhi,[string]$feature,[string]$successText,[bool]$RequireBaseConfig = $false)
    Kill-FortniteProcesses
    $destDir = Get-GUSDir; if (-not (Test-Path $destDir)) { New-Item $destDir -ItemType Directory -Force | Out-Null }
    $settingsFile = Get-GUSPath
    if ($RequireBaseConfig -and -not (Test-Path $settingsFile)) { if (-not (Download-GameUserSettings)) { return } }
    $block = @("[D3DRHIPreference]","PreferredRHI=$rhi","PreferredFeatureLevel=$feature")
    if (Test-Path $settingsFile) {
        try {
            $text = (Get-Content -Path $settingsFile -Raw) -replace "(\r\n|\r)","`n"
            $list = New-Object System.Collections.Generic.List[string]; foreach($ln in ($text -split "`n")){ $list.Add($ln) }
            $start=-1; for($i=0;$i -lt $list.Count;$i++){ if($list[$i] -match '^\s*\[D3DRHIPreference\]\s*$'){ $start=$i; break } }
            if($start -ge 0){ $end=$start+1; while($end -lt $list.Count -and ($list[$end] -notmatch '^\s*\[')){ $end++ }; $list.RemoveRange($start,$end-$start); $insert=$start } else { $insert=$list.Count }
            if($insert -gt 0){ while($insert -gt 0 -and $list[$insert-1].Trim() -eq ''){ $list.RemoveAt($insert-1); $insert-- }; $list.Insert($insert,''); $insert++ }
            foreach($b in $block){ $list.Insert($insert,$b); $insert++ }
            while($insert -lt $list.Count -and $list[$insert].Trim() -eq ''){ $list.RemoveAt($insert) }
            if($insert -lt $list.Count){ $list.Insert($insert,'') } else { $list.Add('') }
            $out = ($list -join "`r`n"); if ($out -notmatch "(\r\n)$") { $out += "`r`n" }
            Set-Content -Path $settingsFile -Value $out -Encoding UTF8
            [void](Show-DarkConfirm -Title "Success" -Text $successText -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 140)
        } catch { [void](Show-DarkConfirm -Title "Error" -Text "Could not modify GameUserSettings.ini." -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 140) }
    } else {
        try { Set-Content -Path $settingsFile -Value (($block -join "`r`n") + "`r`n`r`n") -Encoding UTF8; [void](Show-DarkConfirm -Title "Success" -Text $successText -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 140) }
        catch { [void](Show-DarkConfirm -Title "Error" -Text "Failed to create GameUserSettings.ini." -YesText "OK" -ButtonStyle "OK" -Width 420 -Height 140) }
    }
}

function Apply-DefaultDX11      { Write-GUSValues -rhi "dx11" -feature "sm5"  -successText "Applied: Default DirectX11." }
function Apply-DX11-Performance { Write-GUSValues -rhi "dx11" -feature "es31" -successText "Applied: DirectX11 Performance (recommended)." }
function Apply-DX12-Default     { Write-GUSValues -rhi "dx12" -feature "sm6"  -successText "Applied: DirectX12 Default." -RequireBaseConfig $true }
function Apply-DX12-Performance { Write-GUSValues -rhi "dx12" -feature "es31" -successText "Applied: DirectX12 Performance (experimental)." -RequireBaseConfig $true }

function Create-ModernButton {
    param($text,$description,$y,$icon)
    $btnPanel = New-Object System.Windows.Forms.Panel
    $btnPanel.Size = New-Object System.Drawing.Size(540,70)
    $btnPanel.Location = New-Object System.Drawing.Point(20,$y)
    $btnPanel.BackColor = [System.Drawing.Color]::FromArgb(45,45,55)
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = " $text"; $btn.Size = New-Object System.Drawing.Size(540,40); $btn.Location = New-Object System.Drawing.Point(0,0)
    $btn.FlatStyle='Flat'; try{$btn.FlatAppearance.BorderSize=0}catch{}; $btn.BackColor=[System.Drawing.Color]::FromArgb(55,55,65)
    $btn.ForeColor=[System.Drawing.Color]::White; $btn.Font=$FONT_BUTTON; $btn.TextAlign='MiddleLeft'
    $localBtn=$btn; $localPanel=$btnPanel
    $localBtn.Add_MouseEnter({ param($s,$e) try{$s.BackColor=[System.Drawing.Color]::FromArgb(65,65,75)}catch{}; try{$localPanel.BackColor=[System.Drawing.Color]::FromArgb(55,55,65)}catch{} })
    $localBtn.Add_MouseLeave({ param($s,$e) try{$s.BackColor=[System.Drawing.Color]::FromArgb(55,55,65)}catch{}; try{$localPanel.BackColor=[System.Drawing.Color]::FromArgb(45,45,55)}catch{} })
    $descLabel = New-Object System.Windows.Forms.Label
    $descLabel.Text=$description; $descLabel.Location=New-Object System.Drawing.Point(10,45); $descLabel.Size=New-Object System.Drawing.Size(520,20)
    $descLabel.ForeColor=[System.Drawing.Color]::FromArgb(180,180,180); $descLabel.Font=$FONT_TEXT_SM; try{$descLabel.BackColor=$localPanel.BackColor}catch{$descLabel.BackColor=[System.Drawing.Color]::FromArgb(45,45,55)}
    $btnPanel.Controls.Add($btn); $btnPanel.Controls.Add($descLabel)
    @{ Panel=$btnPanel; Button=$btn }
}

$form = New-Object System.Windows.Forms.Form
$form.Text="FORTNITE UTILITY"; $form.ClientSize=New-Object System.Drawing.Size(620,500); $form.StartPosition="CenterScreen"; $form.FormBorderStyle="FixedDialog"
$form.MaximizeBox=$false; $form.MinimizeBox=$false; $form.BackColor=[System.Drawing.Color]::FromArgb(25,25,30); $form.ForeColor=[System.Drawing.Color]::White
$form.Font=$FONT_TEXT

$headerPanel = New-Object System.Windows.Forms.Panel; $headerPanel.Size=New-Object System.Drawing.Size(620,80); $headerPanel.Location=New-Object System.Drawing.Point(0,0); $headerPanel.BackColor=[System.Drawing.Color]::FromArgb(35,35,45); $form.Controls.Add($headerPanel)
$contentPanel = New-Object System.Windows.Forms.Panel; $contentPanel.BackColor=$headerPanel.BackColor; $headerPanel.Controls.Add($contentPanel)
$titleMain = New-Object System.Windows.Forms.Label; $titleMain.Text="FORTNITE UTILITY BY "; $titleMain.Font=$FONT_TITLE; $titleMain.ForeColor=[System.Drawing.Color]::FromArgb(230,230,230); $titleMain.AutoSize=$true; $titleMain.BackColor=$contentPanel.BackColor; $contentPanel.Controls.Add($titleMain)
$titleLink = New-Object System.Windows.Forms.Label; $titleLink.Text="ARSENZA"; $titleLink.Font=$FONT_TITLE; $titleLink.ForeColor=[System.Drawing.Color]::FromArgb(0,180,255); $titleLink.AutoSize=$true; $titleLink.BackColor=$contentPanel.BackColor; $titleLink.Cursor=[System.Windows.Forms.Cursors]::Hand; $contentPanel.Controls.Add($titleLink)

$GAP_PX=1; try{ $h=[math]::Max($titleMain.PreferredSize.Height,$titleLink.PreferredSize.Height); $w=$titleMain.PreferredSize.Width+$GAP_PX+$titleLink.PreferredSize.Width; $contentPanel.Size=New-Object System.Drawing.Size($w,$h); $titleMain.Location=New-Object System.Drawing.Point(0,[int](($h-$titleMain.PreferredSize.Height)/2)); $titleLink.Location=New-Object System.Drawing.Point(($titleMain.PreferredSize.Width+$GAP_PX),[int](($h-$titleLink.PreferredSize.Height)/2)) } catch { $contentPanel.Size=New-Object System.Drawing.Size(420,40) }
$contentPanel.Location = New-Object System.Drawing.Point([int](($headerPanel.ClientSize.Width-$contentPanel.Width)/2), [int](($headerPanel.ClientSize.Height-$contentPanel.Height)/2))
$titleLink.Add_MouseEnter({ param($s,$e) try{$s.Font=New-Object System.Drawing.Font($s.Font, $s.Font.Style -bor [System.Drawing.FontStyle]::Underline)}catch{}; try{$s.ForeColor=[System.Drawing.Color]::FromArgb(80,200,255)}catch{} })
$titleLink.Add_MouseLeave({ param($s,$e) try{$s.Font=$FONT_TITLE}catch{}; try{$s.ForeColor=[System.Drawing.Color]::FromArgb(0,180,255)}catch{} })
$titleLink.Add_Click({ try { Start-Process "https://x.com/arsenzaa" } catch {} })

$mainPanel = New-Object System.Windows.Forms.Panel; $mainPanel.Size=New-Object System.Drawing.Size(580,350); $mainPanel.Location=New-Object System.Drawing.Point(20,90); $mainPanel.BackColor=[System.Drawing.Color]::FromArgb(35,35,45); $form.Controls.Add($mainPanel)

$linkLabel = New-Object System.Windows.Forms.LinkLabel; $prefixText="BUY HYPE MODE SCRIPT AND TWEAKING YOUR COMPUTER - "; $url="https://dsc.gg/hypetweaks"; $linkLabel.Text=$prefixText+$url
$linkLabel.Location=New-Object System.Drawing.Point(20,15); $linkLabel.Size=New-Object System.Drawing.Size(540,20); $linkLabel.LinkColor=[System.Drawing.Color]::FromArgb(0,150,255); $linkLabel.ActiveLinkColor=[System.Drawing.Color]::FromArgb(0,200,255); $linkLabel.Font=$FONT_TEXT_SM
$linkLabel.LinkArea=New-Object System.Windows.Forms.LinkArea ($prefixText.Length,$url.Length); $linkLabel.Links.Clear(); [void]$linkLabel.Links.Add($prefixText.Length,$url.Length,$url)
$linkLabel.Add_LinkClicked({ param($s,$e) try{ $target = if ($e -and $e.Link -and $e.Link.LinkData) {[string]$e.Link.LinkData}else{$url}; Start-Process $target }catch{} })
$mainPanel.Controls.Add($linkLabel)

$btnClear = Create-ModernButton "Clear Fortnite Cache" "Recompilation of shaders (recommended after global updates in the game)" 50 ""
$btnClear.Button.Font = $FONT_BUTTON
$mainPanel.Controls.Add($btnClear.Panel)
$btnClear.Button.Add_Click({
    $warn="WARNING:`nClearing the Fortnite cache will trigger shader recompilation.`nOn the first launch of the game, you may experience stutters, and FPS drops. Everything will stabilize after a while.`n`nDo you want to continue?"
    if ((Show-DarkConfirm -Title "Confirm cache clear" -Text $warn -YesText "Yes" -NoText "No" -ButtonStyle "YesNo" -Width 520 -Height 190) -eq [System.Windows.Forms.DialogResult]::Yes) { Clear-Cache }
})

$btnInstall = Create-ModernButton "Install Fortnite Graphics Settings" "GameUserSettings.ini" 130 ""
$btnInstall.Button.Font = $FONT_BUTTON
$btnInstall.Button.Add_Click({
    $dlg = New-Object System.Windows.Forms.Form
    $dlg.Text="Install Settings"; $dlg.StartPosition="CenterParent"; $dlg.FormBorderStyle="FixedDialog"
    $dlg.BackColor=[System.Drawing.Color]::FromArgb(30,30,30); $dlg.ForeColor=[System.Drawing.Color]::White; $dlg.MinimizeBox=$false; $dlg.MaximizeBox=$false; $dlg.Font=$FONT_TEXT; $dlg.AutoScaleMode=[System.Windows.Forms.AutoScaleMode]::Dpi
    [int]$MARGIN_X=20; [int]$ROW_GAP=16; [int]$PAIR_W=230; [int]$PAIR_H=42; [int]$GUTTER=12
    [int]$dlgW = 2*$MARGIN_X + 2*$PAIR_W + $GUTTER
    $dlg.ClientSize = New-Object System.Drawing.Size($dlgW,260)
    [int]$CENTER_X=[int]($dlg.ClientSize.Width/2)

    $desc = New-Object System.Windows.Forms.Label
    $desc.AutoSize=$true; $desc.Location=New-Object System.Drawing.Point($MARGIN_X,10); $desc.Text="Select Rendering Mode:"; $desc.ForeColor=[System.Drawing.Color]::White; $desc.BackColor=$dlg.BackColor; $desc.Font=$FONT_TEXT_SM
    $dlg.Controls.Add($desc)

    $btnDownloadGUS = New-Object System.Windows.Forms.Button
    $btnDownloadGUS.Size=New-Object System.Drawing.Size([int]($dlg.ClientSize.Width-2*$MARGIN_X),40)
    $btnDownloadGUS.Location=New-Object System.Drawing.Point($MARGIN_X,[int]($desc.Bottom+10))
    $btnDownloadGUS.FlatStyle='Flat'; try{$btnDownloadGUS.FlatAppearance.BorderSize=0}catch{}; $btnDownloadGUS.ForeColor=[System.Drawing.Color]::White
    $btnDownloadGUS.Font=$FONT_BUTTON; $btnDownloadGUS.TextAlign="MiddleCenter"; $btnDownloadGUS.TabStop=$false
    $btnDownloadGUS.Text="Install GameUserSettings.ini"
    $gusFile=Get-GUSPath
    if (Test-Path $gusFile) { $btnDownloadGUS.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $btnDownloadGUS.Text="Install GameUserSettings.ini (installed)" }
    else { $btnDownloadGUS.BackColor=[System.Drawing.Color]::FromArgb(60,120,200) }
    $btnDownloadGUS.Add_Click({
        $exists = Test-Path (Get-GUSPath)
        if ($exists) {
            $res=Show-DarkConfirm -Title "Confirm" -Text "GameUserSettings.ini already exists.`nOverwrite it with the latest from GitHub?" -YesText "Overwrite" -NoText "Cancel" -ButtonStyle "YesNo" -Width 500 -Height 160
            if ($res -ne [System.Windows.Forms.DialogResult]::Yes) { return }
        }
        if (Download-GameUserSettings) { $this.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $this.Text="Install GameUserSettings.ini (installed)" }
    })
    $dlg.Controls.Add($btnDownloadGUS)

    [int]$rowY = $btnDownloadGUS.Bottom + $ROW_GAP

    $btnDX11Main = New-Object System.Windows.Forms.Button
    $btnDX11Main.Text="DirectX11 (recommended)"; $btnDX11Main.Size=New-Object System.Drawing.Size($PAIR_W,$PAIR_H)
    $btnDX11Main.Location=New-Object System.Drawing.Point([int]($CENTER_X - ($GUTTER/2) - $PAIR_W), $rowY)
    $btnDX11Main.FlatStyle='Flat'; try{$btnDX11Main.FlatAppearance.BorderSize=0}catch{}
    $btnDX11Main.BackColor=[System.Drawing.Color]::FromArgb(60,120,200); $btnDX11Main.ForeColor=[System.Drawing.Color]::White
    $btnDX11Main.Font=$FONT_BUTTON; $btnDX11Main.TextAlign="MiddleCenter"
    $btnDX11Main.Add_Click({
        $dlg11=New-Object System.Windows.Forms.Form
        $dlg11.Text="DirectX11 Options"; $dlg11.StartPosition="CenterParent"; $dlg11.FormBorderStyle="FixedDialog"
        $dlg11.BackColor=[System.Drawing.Color]::FromArgb(30,30,30); $dlg11.ForeColor=[System.Drawing.Color]::White; $dlg11.MinimizeBox=$false; $dlg11.MaximizeBox=$false; $dlg11.Font=$FONT_TEXT; $dlg11.AutoScaleMode=[System.Windows.Forms.AutoScaleMode]::Dpi
        [int]$M=20; [int]$W=230; [int]$H=36; [int]$G=12; [int]$dlgW2=2*$M+2*$W+$G
        $dlg11.ClientSize=New-Object System.Drawing.Size($dlgW2,190)
        [int]$CX=[int]($dlg11.ClientSize.Width/2)
        $l=New-Object System.Windows.Forms.Label; $l.AutoSize=$true; $l.Location=New-Object System.Drawing.Point($M,20); $l.Text="Choose DX11 profile to apply:"; $l.Font=$FONT_TEXT_SM; $dlg11.Controls.Add($l)
        $b1=New-Object System.Windows.Forms.Button; $b1.Text="DX11 Default"; $b1.Size=New-Object System.Drawing.Size($W,$H); $b1.Location=New-Object System.Drawing.Point([int]($CX-$G/2-$W),90)
        $b1.FlatStyle='Flat'; try{$b1.FlatAppearance.BorderSize=0}catch{}; $b1.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $b1.ForeColor=[System.Drawing.Color]::White; $b1.Font=$FONT_TEXT; $b1.TextAlign="MiddleCenter"; $b1.Add_Click({ Apply-DefaultDX11; $dlg11.Close(); $dlg.Close() }); $dlg11.Controls.Add($b1)
        $b2=New-Object System.Windows.Forms.Button; $b2.Text="DX11 Performance (recommended)"; $b2.Size=New-Object System.Drawing.Size($W,$H); $b2.Location=New-Object System.Drawing.Point([int]($CX+$G/2),90)
        $b2.FlatStyle='Flat'; try{$b2.FlatAppearance.BorderSize=0}catch{}; $b2.BackColor=[System.Drawing.Color]::FromArgb(60,120,200); $b2.ForeColor=[System.Drawing.Color]::White; $b2.Font=$FONT_BUTTON; $b2.TextAlign="MiddleCenter"; $b2.Add_Click({ Apply-DX11-Performance; $dlg11.Close(); $dlg.Close() }); $dlg11.Controls.Add($b2)

        # --- Cancel button for DX11 Options ---
        $cancel11 = New-Object System.Windows.Forms.Button
        $cancel11.Text="Cancel"; $cancel11.Size=New-Object System.Drawing.Size(100,30)
        $cancel11.Location=New-Object System.Drawing.Point([int]($CX - $cancel11.Width/2), [int]($b1.Bottom + 24))
        $cancel11.FlatStyle='Flat'; try{$cancel11.FlatAppearance.BorderSize=0}catch{}
        $cancel11.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $cancel11.ForeColor=[System.Drawing.Color]::White; $cancel11.Font=$FONT_TEXT
        $cancel11.Add_Click({ $dlg11.Close() })
        $dlg11.Controls.Add($cancel11)
        $dlg11.CancelButton = $cancel11
        $dlg11.ClientSize = New-Object System.Drawing.Size($dlgW2, [int]($cancel11.Bottom + 20))
        [void]$dlg11.ShowDialog($dlg)
    })
    $dlg.Controls.Add($btnDX11Main)

    $btnDX12Main = New-Object System.Windows.Forms.Button
    $btnDX12Main.Text="DirectX12 (experimental)"; $btnDX12Main.Size=New-Object System.Drawing.Size($PAIR_W,$PAIR_H)
    $btnDX12Main.Location=New-Object System.Drawing.Point([int]($CENTER_X + ($GUTTER/2)), $rowY)
    $btnDX12Main.FlatStyle='Flat'; try{$btnDX12Main.FlatAppearance.BorderSize=0}catch{}
    $btnDX12Main.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $btnDX12Main.ForeColor=[System.Drawing.Color]::White
    $btnDX12Main.Font=$FONT_TEXT; $btnDX12Main.TextAlign="MiddleCenter"
    $btnDX12Main.Add_Click({
        $dlg12=New-Object System.Windows.Forms.Form
        $dlg12.Text="DirectX12 Options"; $dlg12.StartPosition="CenterParent"; $dlg12.FormBorderStyle="FixedDialog"
        $dlg12.BackColor=[System.Drawing.Color]::FromArgb(30,30,30); $dlg12.ForeColor=[System.Drawing.Color]::White; $dlg12.MinimizeBox=$false; $dlg12.MaximizeBox=$false; $dlg12.Font=$FONT_TEXT; $dlg12.AutoScaleMode=[System.Windows.Forms.AutoScaleMode]::Dpi
        [int]$M=20; [int]$W=230; [int]$H=36; [int]$G=12; [int]$dlgW3=2*$M+2*$W+$G
        $dlg12.ClientSize=New-Object System.Drawing.Size($dlgW3,190)
        [int]$CX=[int]($dlg12.ClientSize.Width/2)
        $l2=New-Object System.Windows.Forms.Label; $l2.AutoSize=$true; $l2.Location=New-Object System.Drawing.Point($M,20); $l2.Text="Choose DX12 profile to apply:"; $l2.Font=$FONT_TEXT_SM; $dlg12.Controls.Add($l2)
        $b3=New-Object System.Windows.Forms.Button; $b3.Text="DX12 Default"; $b3.Size=New-Object System.Drawing.Size($W,$H); $b3.Location=New-Object System.Drawing.Point([int]($CX-$G/2-$W),90)
        $b3.FlatStyle='Flat'; try{$b3.FlatAppearance.BorderSize=0}catch{}; $b3.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $b3.ForeColor=[System.Drawing.Color]::White; $b3.Font=$FONT_TEXT; $b3.TextAlign="MiddleCenter"; $b3.Add_Click({ Apply-DX12-Default; $dlg12.Close(); $dlg.Close() }); $dlg12.Controls.Add($b3)
        $b4=New-Object System.Windows.Forms.Button; $b4.Text="DX12 Performance (experimental)"; $b4.Size=New-Object System.Drawing.Size($W,$H); $b4.Location=New-Object System.Drawing.Point([int]($CX+$G/2),90)
        $b4.FlatStyle='Flat'; try{$b4.FlatAppearance.BorderSize=0}catch{}
        # make it gray (not blue) like others
        $b4.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $b4.ForeColor=[System.Drawing.Color]::White; $b4.Font=$FONT_TEXT
        $b4.TextAlign="MiddleCenter"; $b4.Add_Click({ Apply-DX12-Performance; $dlg12.Close(); $dlg.Close() }); $dlg12.Controls.Add($b4)

        # --- Cancel button for DX12 Options ---
        $cancel12 = New-Object System.Windows.Forms.Button
        $cancel12.Text="Cancel"; $cancel12.Size=New-Object System.Drawing.Size(100,30)
        $cancel12.Location=New-Object System.Drawing.Point([int]($CX - $cancel12.Width/2), [int]($b3.Bottom + 24))
        $cancel12.FlatStyle='Flat'; try{$cancel12.FlatAppearance.BorderSize=0}catch{}
        $cancel12.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $cancel12.ForeColor=[System.Drawing.Color]::White; $cancel12.Font=$FONT_TEXT
        $cancel12.Add_Click({ $dlg12.Close() })
        $dlg12.Controls.Add($cancel12)
        $dlg12.CancelButton = $cancel12
        $dlg12.ClientSize = New-Object System.Drawing.Size($dlgW3, [int]($cancel12.Bottom + 20))
        [void]$dlg12.ShowDialog($dlg)
    })
    $dlg.Controls.Add($btnDX12Main)

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text="Cancel"; $cancel.Size=New-Object System.Drawing.Size(100,30)
    $cancel.Location=New-Object System.Drawing.Point([int]($CENTER_X - $cancel.Width/2), [int]($btnDX11Main.Bottom + 24))
    $cancel.FlatStyle='Flat'; try{$cancel.FlatAppearance.BorderSize=0}catch{}; $cancel.BackColor=[System.Drawing.Color]::FromArgb(70,70,70); $cancel.ForeColor=[System.Drawing.Color]::White; $cancel.Font=$FONT_TEXT
    $cancel.Add_Click({ $dlg.Close() }); $dlg.Controls.Add($cancel)

    $dlg.ClientSize = New-Object System.Drawing.Size($dlgW, [int]($cancel.Bottom + 20))
    [void]$dlg.ShowDialog($form)
})
$mainPanel.Controls.Add($btnInstall.Panel)

$btnExit = Create-ModernButton "Exit Fortnite Utility" "Close the application" 210 ""
$btnExit.Button.Font = $FONT_BUTTON
$btnExit.Button.Add_Click({ $form.Close() })
$mainPanel.Controls.Add($btnExit.Panel)

$controlsBottom = 0; foreach ($ctl in $mainPanel.Controls) { $b = $ctl.Location.Y + $ctl.Size.Height; if ($b -gt $controlsBottom) { $controlsBottom = $b } }
$INNER_BOTTOM_PADDING = 14; $mainPanel.Height = [math]::Ceiling($controlsBottom + $INNER_BOTTOM_PADDING)

$footerLink = New-Object System.Windows.Forms.LinkLabel
$footerLink.Text = "Version: $((Read-VersionFile).ver)"; $footerLink.Font = $FONT_TEXT_SM
$footerLink.ForeColor=[System.Drawing.Color]::FromArgb(150,150,150); $footerLink.LinkColor=[System.Drawing.Color]::FromArgb(120,180,255); $footerLink.ActiveLinkColor=[System.Drawing.Color]::FromArgb(160,210,255); $footerLink.VisitedLinkColor=[System.Drawing.Color]::FromArgb(120,180,255)
$footerLink.Size=New-Object System.Drawing.Size(580,20); $BOTTOM_MARGIN=40
$footerLink.Location=New-Object System.Drawing.Point(20, ($form.ClientSize.Height - $BOTTOM_MARGIN - $footerLink.Size.Height))
$footerLink.TextAlign="MiddleCenter"; $footerLink.Anchor=([System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right)
$footerLink.Links.Clear(); [void]$footerLink.Links.Add(0,$footerLink.Text.Length,"https://github.com/arsenzaaa/FORTNITE-UTILITY")
$footerLink.Add_LinkClicked({ param($s,$e) try{ $target = if ($e -and $e.Link -and $e.Link.LinkData) {[string]$e.Link.LinkData}else{"https://github.com/arsenzaaa/FORTNITE-UTILITY"}; Start-Process $target }catch{} })
$form.Controls.Add($footerLink)

[void]$form.ShowDialog()
