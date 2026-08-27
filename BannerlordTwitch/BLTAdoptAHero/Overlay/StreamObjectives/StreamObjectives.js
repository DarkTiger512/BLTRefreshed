$(document).ready(function () {
    const panel = document.getElementById('stream-objective');
    if (!panel || typeof $.connection.streamObjectivesHub === 'undefined') return;
    const hub = $.connection.streamObjectivesHub;
    const render = function (data) {
        if (!data || data.Version !== 1 || !data.Visible) {
            panel.classList.add('hidden'); panel.setAttribute('aria-hidden', 'true'); return;
        }
        const percent = data.Target > 0 ? Math.min(100, Math.round(data.Progress * 100 / data.Target)) : 0;
        panel.classList.remove('hidden'); panel.setAttribute('aria-hidden', 'false');
        panel.style.setProperty('--objective-opacity', Math.max(0, Math.min(1, data.Opacity || .92)));
        panel.style.setProperty('--objective-width', `${Math.max(20, Math.min(60, data.WidthPercent || 38))}vw`);
        document.getElementById('objective-percent').textContent = `${percent}%`;
        document.getElementById('objective-description').textContent = data.Description || 'Community objective';
        document.getElementById('objective-progress').style.width = `${percent}%`;
        document.getElementById('objective-count').textContent = `${data.Progress}/${data.Target}`;
        document.getElementById('objective-reward').textContent = `${data.Gold} gold · ${data.XP} XP`;
        const list = document.getElementById('objective-contributors'); list.innerHTML = '';
        (data.Contributors || []).forEach(c => {
            const li = document.createElement('li'); li.textContent = `${c.Name} ${c.Detail}`; list.appendChild(li);
        });
    };
    hub.client.updateObjective = render;
    $.connection.hub.start().done(function () { hub.server.refresh().done(render); });
});
