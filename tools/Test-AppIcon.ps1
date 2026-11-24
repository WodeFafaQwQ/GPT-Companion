param(
    [string]$ExecutablePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist/DragonCompanion.exe'),
    [string]$IconPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'assets/app.ico'),
    [string]$PreviewDirectory = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not ('CompanionIconNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class CompanionIconNative {
    delegate bool ResourceNameCallback(IntPtr module, IntPtr type, IntPtr name, IntPtr parameter);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll")] public static extern bool FreeLibrary(IntPtr module);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool EnumResourceNames(IntPtr module, IntPtr type, ResourceNameCallback callback, IntPtr parameter);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="PrivateExtractIconsW")] public static extern uint ExtractIcons(string file, int index, int width, int height, IntPtr[] handles, uint[] ids, uint count, uint flags);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
    public static int[] GroupIds(IntPtr module) {
        var ids = new List<int>();
        ResourceNameCallback callback = delegate(IntPtr m, IntPtr t, IntPtr n, IntPtr p) {
            if (n.ToInt64() > 65535) throw new InvalidOperationException("Expected numbered compiler icon resource.");
            ids.Add(n.ToInt32()); return true;
        };
        EnumResourceNames(module, new IntPtr(14), callback, IntPtr.Zero); return ids.ToArray();
    }
    public static byte[] Resource(IntPtr module, int type, int id) {
        var resource = FindResource(module, new IntPtr(id), new IntPtr(type));
        if (resource == IntPtr.Zero) throw new InvalidOperationException("Missing Win32 icon resource " + id);
        var bytes = new byte[SizeofResource(module, resource)];
        Marshal.Copy(LockResource(LoadResource(module, resource)), bytes, 0, bytes.Length); return bytes;
    }
}
'@
}
if ($PreviewDirectory) { New-Item -ItemType Directory -Path $PreviewDirectory -Force | Out-Null }
$iconBytes = [IO.File]::ReadAllBytes($IconPath)
$expectedSizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256)
$count = [BitConverter]::ToUInt16($iconBytes, 4)
if ($count -ne $expectedSizes.Count) { throw "Expected $($expectedSizes.Count) ICO frames; got $count." }
$module = [CompanionIconNative]::LoadLibraryEx($ExecutablePath, [IntPtr]::Zero, 0x22)
if ($module -eq [IntPtr]::Zero) { throw "Cannot inspect EXE resources: $ExecutablePath" }
$rows = @()
try {
    $groupIds = @([CompanionIconNative]::GroupIds($module))
    if ($groupIds.Count -ne 1) { throw "Expected one embedded icon group; got $($groupIds.Count)." }
    $group = [CompanionIconNative]::Resource($module, 14, $groupIds[0])
    if ([BitConverter]::ToUInt16($group, 4) -ne $count) { throw 'Embedded icon frame count differs from app.ico.' }
    for ($n = 0; $n -lt $count; $n++) {
        $size = $expectedSizes[$n]
        $icoOffset = 6 + 16 * $n
        $groupOffset = 6 + 14 * $n
        $width = if ($iconBytes[$icoOffset] -eq 0) { 256 } else { [int]$iconBytes[$icoOffset] }
        if ($width -ne $size -or $iconBytes[$icoOffset + 1] -ne $iconBytes[$icoOffset]) { throw "Incorrect ICO dimensions at frame $n." }
        for ($i = 0; $i -lt 12; $i++) { if ($iconBytes[$icoOffset + $i] -ne $group[$groupOffset + $i]) { throw "Embedded icon descriptor differs at ${size}px." } }
        if ([BitConverter]::ToUInt16($iconBytes, $icoOffset + 4) -ne 1 -or [BitConverter]::ToUInt16($iconBytes, $icoOffset + 6) -ne 32) { throw "Incorrect ICO bit depth at ${size}px." }
        $length = [BitConverter]::ToInt32($iconBytes, $icoOffset + 8)
        $dataOffset = [BitConverter]::ToInt32($iconBytes, $icoOffset + 12)
        $resourceId = [BitConverter]::ToUInt16($group, $groupOffset + 12)
        $resourceBytes = [CompanionIconNative]::Resource($module, 3, $resourceId)
        if ($resourceBytes.Length -ne $length) { throw "Wrong Win32 icon resource length at ${size}px." }
        for ($i = 0; $i -lt $length; $i++) { if ($resourceBytes[$i] -ne $iconBytes[$dataOffset + $i]) { throw "Win32 icon resource differs from app.ico at ${size}px." } }
        $handles = [IntPtr[]]::new(1)
        if ([CompanionIconNative]::ExtractIcons($ExecutablePath, 0, $size, $size, $handles, [uint32[]]::new(1), 1, 0) -ne 1 -or $handles[0] -eq [IntPtr]::Zero) { throw "Windows cannot extract the ${size}px icon." }
        try {
            $icon = [Drawing.Icon]::FromHandle($handles[0])
            $bitmap = $icon.ToBitmap()
            try {
                if ($bitmap.Width -ne $size -or $bitmap.Height -ne $size) { throw "Windows extracted an incorrectly sized ${size}px icon." }
                $visible = 0; $transparent = 0; $partial = 0
                for ($y = 0; $y -lt $size; $y++) { for ($x = 0; $x -lt $size; $x++) {
                    $alpha = $bitmap.GetPixel($x, $y).A
                    if ($alpha -ge 128) { $visible++ }
                    if ($alpha -eq 0) { $transparent++ }
                    if ($alpha -gt 0 -and $alpha -lt 255) { $partial++ }
                } }
                if ($visible -lt $size * $size / 2 -or $transparent -eq 0 -or $partial -eq 0) { throw "Icon pixels/alpha are invalid at ${size}px." }
                if ($PreviewDirectory) { $bitmap.Save((Join-Path $PreviewDirectory "exe-icon-$size.png"), [Drawing.Imaging.ImageFormat]::Png) }
                $rows += [pscustomobject]@{ Size = "${size}x${size}"; Bpp = 32; ResourceId = $resourceId; ResourceBytes = $length; VisiblePixels = $visible; TransparentPixels = $transparent; PartialAlphaPixels = $partial }
            } finally { $bitmap.Dispose(); $icon.Dispose() }
        } finally { [CompanionIconNative]::DestroyIcon($handles[0]) | Out-Null }
    }
} finally { [CompanionIconNative]::FreeLibrary($module) | Out-Null }
$rows | Format-Table -AutoSize
Write-Output "PASS: RT_GROUP_ICON=$($groupIds[0]); all 10 RT_ICON resources match app.ico and Windows extracts visible GPT artwork at every size."
