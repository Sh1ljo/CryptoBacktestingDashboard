$rawInput = $input | Out-String
$logPath = "C:\Users\gabys\Desktop\Programiranje\CryptoBacktestingDashboard\.github\hooks\agent_log.md"

try {
    $obj = $rawInput | ConvertFrom-Json
    $logEntry = @{
        timestamp  = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        event      = $obj.hook_event_name
        agent_type = $obj.agent_type
        agent_id   = $obj.agent_id
        session_id = $obj.session_id
        cwd        = $obj.cwd
    } | ConvertTo-Json -Depth 5
    $logEntry = $logEntry -replace '\\u003c','<' -replace '\\u003e','>' -replace '\\u0027',"'"
    Add-Content -Path $logPath -Value "[SUBAGENT] $logEntry"
} catch {
    Add-Content -Path $logPath -Value "[SUBAGENT] $rawInput"
}
Add-Content -Path $logPath -Value ""

exit 0