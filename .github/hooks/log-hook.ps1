param([string]$LogFile = "C:\Users\gabys\Desktop\Programiranje\CryptoBacktestingDashboard\CryptoBacktestingDashboard\lab-1\agent_log.md")

$raw = [Console]::In.ReadToEnd()

if ([string]::IsNullOrWhiteSpace($raw)) {
    exit 0
}

try {
    $json = $raw | ConvertFrom-Json
}
catch {
    # Replaced ``` with `````` to properly output 3 literal backticks
    Add-Content -Path $LogFile -Value "`n---`n`n**Raw hook input**`n`n``````json`n$raw`n``````" -Encoding utf8
    exit 0
}

$timestamp = $json.timestamp
$eventName = $json.hook_event_name

$prompt = "No prompt"
if ($null -ne $json.prompt -and "$($json.prompt)" -ne "") {
    $prompt = "$($json.prompt)"
}
elseif ($null -ne $json.sessionId -and "$($json.sessionId)" -ne "") {
    $prompt = "$($json.sessionId)"
}

$toolName = ""
if ($null -ne $json.tool_name -and "$($json.tool_name)" -ne "") {
    $toolName = "$($json.tool_name)"
}

$formatted = "`n---`n`n**[$timestamp UTC] $eventName**`n`nPrompt: $prompt`n"

if ($toolName -ne "") {
    $formatted += "`nTool: $toolName`n"
}

# Replaced ``` with `````` to properly output 3 literal backticks
$formatted += "`n<details>`n<summary>Raw JSON</summary>`n`n``````json`n"
$formatted += ($json | ConvertTo-Json -Depth 10)
$formatted += "`n`````` `n</details>`n"

Add-Content -Path $LogFile -Value $formatted -Encoding utf8