$sourceRoot = 'C:\Users\conta\.codex\generated_images\01a163e4-1fe6-7e32-93da-7be0e4842dc5'
$stage = Join-Path $PSScriptRoot 'imagegen-input'
New-Item -ItemType Directory -Force -Path $stage | Out-Null

$map = [ordered]@{
    'mind.png'       = 'exec-943bc205-cd3c-4d60-a1bb-c7468085629c.png'
    'logic.png'      = 'exec-52afdab0-93e3-4a1d-8908-da37642a9d73.png'
    'org.png'        = 'exec-3ad071f3-5028-46c9-9524-51e55e0c9953.png'
    'tree.png'       = 'exec-ff2167f6-4246-409b-b26f-88cef8ab73a2.png'
    'timeline.png'   = 'exec-33287ca0-0e20-48a8-8bd6-9aa624d0b80b.png'
    'vtimeline.png'  = 'exec-f94ad8ff-7e5c-4ce5-8d05-6773fd1cf41e.png'
    'fishbone.png'   = 'exec-a52cb4ab-2c1d-4503-a768-7543bdc219f2.png'
    'brainstorm.png' = 'exec-ab12b49d-6659-4612-97d3-3df8e31ec84c.png'
    'project.png'    = 'exec-43c47ad9-1619-4127-9766-6bfd97259900.png'
    'meeting.png'    = 'exec-0740f867-f225-47bb-a487-c9b0ce0ed097.png'
    'swot.png'       = 'exec-2d0bea82-aaaf-47d4-8fb2-8c704b4fdd50.png'
    'week.png'       = 'exec-75e7afed-a5fc-47f9-afb0-c89c30e1801b.png'
    'orgup.png'      = 'exec-4b745480-de44-4ff8-a5c3-1aefeca7427f.png'
}

foreach ($item in $map.GetEnumerator()) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $item.Value) -Destination (Join-Path $stage $item.Key) -Force
}

$destination = Join-Path (Split-Path $PSScriptRoot -Parent) 'templates'
& (Join-Path $PSScriptRoot 'Resize-Templates.ps1') -SourceDirectory $stage -DestinationDirectory $destination

# The staging copies are temporary; retain only the two delivery scripts.
Remove-Item -LiteralPath $stage -Recurse -Force
