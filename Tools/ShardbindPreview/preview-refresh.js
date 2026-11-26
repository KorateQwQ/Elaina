const stateKey = `shardbind-view:${location.pathname}`;

export function restoreViewState() {
    const saved = sessionStorage.getItem(stateKey);
    sessionStorage.removeItem(stateKey);
    return saved ? JSON.parse(saved) : null;
}

export function watchBuild(manifest, current, saveState, status) {
    if (!current.buildId) throw new Error('预览缺少构建编号，请重新运行 rebuild。');
    const updated = new Date(current.generated).toLocaleTimeString('zh-CN', {hour12: false});
    const frames = current.update?.frames || [];
    const ranges = [];
    for (const tick of frames) {
        const last = ranges.at(-1);
        if (last && tick === last[1] + 1) last[1] = tick;
        else ranges.push([tick, tick]);
    }
    const scope = current.update?.mode === 'partial'
        ? `本次更新 ${ranges.map(([a,b])=>a===b?a:`${a}–${b}`).join('、')} 帧`
        : '整段更新';
    status.textContent = `更新于 ${updated} · ${scope} · 自动刷新已开启`;
    status.title = `构建 ${current.buildId}`;
    let checking = false, timer;

    // Only completed builds publish a new ID. Reloading also discards ImageBitmap/blob caches.
    async function check() {
        if (checking) return;
        checking = true;
        clearTimeout(timer);
        if (!document.hidden) {
            const response = await fetch(manifest, {cache: 'no-store'});
            if (!response.ok) throw new Error(`检查预览更新失败：${response.status}`);
            const next = await response.json();
            if (!next.buildId) throw new Error('预览构建编号无效。');
            if (next.buildId !== current.buildId) {
                status.textContent = '正在载入新预览…';
                sessionStorage.setItem(stateKey, JSON.stringify(saveState()));
                const url = new URL(location.href);
                url.searchParams.set('build', next.buildId);
                location.replace(url.href);
                return;
            }
        }
        checking = false;
        timer = setTimeout(check, 1000);
    }
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) void check();
    });
    timer = setTimeout(check, 1000);
}
