function ConvertTo-ShardbindFrames([string]$Text) {
    if ([string]::IsNullOrWhiteSpace($Text)) { return }
    $ticks = foreach ($part in ($Text -split '[,，\s]+' | Where-Object { $_ })) {
        if ($part -notmatch '^(\d{1,3})(?:-(\d{1,3}))?$') {
            throw "帧格式无效：$part。示例：176,194,211 或 190-212。"
        }
        $first = [int]$Matches[1]
        $last = if ($Matches[2]) { [int]$Matches[2] } else { $first }
        if ($first -gt $last -or $last -gt 277) { throw "帧范围无效：$part。请输入 0–277 内的帧号或递增范围。" }
        $first..$last
    }
    if (!$ticks) { throw '请输入至少一个帧号。' }
    $ticks | Sort-Object -Unique
}
