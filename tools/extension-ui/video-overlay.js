/* global Twitch */
(function () {
  const authStatus = document.getElementById('authStatus');
  const ctxStatus = document.getElementById('ctxStatus');
  const btnAction = document.getElementById('btnAction');

  const STATE = {
    token: null,
    channelId: null,
    role: null,
    userId: null,
  };

  const EXT_EBS_BASE_URL = 'https://your-ebs.example.com'; // TODO: replace for real EBS
  const EBS_CONFIGURED = typeof EXT_EBS_BASE_URL === 'string' && !/your-ebs\.example\.com/i.test(EXT_EBS_BASE_URL);

  function setAuthStatus(text) {
    authStatus.textContent = `Auth: ${text}`;
  }
  function setCtxStatus(text) {
    ctxStatus.textContent = `Ctx: ${text}`;
  }

  async function sendAction(action, payload = {}) {
    if (!STATE.token || !EBS_CONFIGURED) return;
    try {
      const res = await fetch(`${EXT_EBS_BASE_URL}/ext/action`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${STATE.token}` },
        body: JSON.stringify({ action, payload }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
    } catch (e) {
      // Keep overlay quiet; this is just a stub.
      // Consider adding a small toast on failure for UX later.
    }
  }

  function onAuthorized(auth) {
    STATE.token = auth.token;
    STATE.channelId = auth.channelId;
    try {
      const payload = JSON.parse(atob(auth.token.split('.')[1] || '')) || {};
      STATE.role = payload.role || null;
      STATE.userId = payload.user_id || null;
    } catch {}
    setAuthStatus(`${STATE.userId ? `user ${STATE.userId}` : 'anon'} • ${STATE.role || 'viewer'}`);

    if (EBS_CONFIGURED) {
      btnAction.disabled = false;
    } else {
      btnAction.disabled = true;
      btnAction.title = 'Configure EXT_EBS_BASE_URL in video-overlay.js';
    }
  }

  function onContext(ctx, changed) {
    const parts = [];
    if (changed.includes('theme') && ctx.theme) parts.push(ctx.theme);
    if (changed.includes('isFullscreen')) parts.push(`fs:${ctx.isFullscreen ? 'y' : 'n'}`);
    if (changed.includes('arePlayerControlsVisible')) parts.push(`ctl:${ctx.arePlayerControlsVisible ? 'y' : 'n'}`);
    if (changed.includes('displayResolution') && ctx.displayResolution) parts.push(ctx.displayResolution);
    if (parts.length) setCtxStatus(parts.join(' • '));
  }

  function onVisibilityChanged(isVisible) {
    // Optional: dim or hide overlay UI when not visible
    document.body.style.opacity = isVisible ? '1' : '0.3';
  }

  function init() {
    if (!window.Twitch || !window.Twitch.ext) {
      setAuthStatus('Not in Twitch iframe');
      return;
    }
    Twitch.ext.onAuthorized(onAuthorized);
    Twitch.ext.onContext(onContext);
    Twitch.ext.onVisibilityChanged(onVisibilityChanged);

    // Receive overlay state via Extension PubSub (broadcast)
    Twitch.ext.listen('broadcast', (topic, message) => {
      try {
        const data = JSON.parse(message);
        // Basic dev feedback in the HUD; replace with real renderer later
        if (data && data.type === 'overlay_state') {
          // If the overlay_state is a duel, show a transient toast
          try {
            const st = data.state || {};
            if (st && st.type === 'duel') {
              const txt = `${st.challenger} vs ${st.opponent}`;
              const n = document.createElement('div');
              n.textContent = txt;
              n.style.position = 'absolute';
              n.style.top = '20px';
              n.style.left = '50%';
              n.style.transform = 'translateX(-50%)';
              n.style.background = 'rgba(0,0,0,0.6)';
              n.style.color = '#fff';
              n.style.padding = '8px 12px';
              n.style.borderRadius = '10px';
              n.style.pointerEvents = 'none';
              document.body.appendChild(n);
              setTimeout(() => n.remove(), 3000);
            } else {
              setCtxStatus(`state:${JSON.stringify(data.state) || 'update'}`);
            }
          } catch (e) { setCtxStatus('state:update'); }
        } else if (data && data.type === 'toast') {
          // Example of a small visual cue
          const n = document.createElement('div');
          n.textContent = data.text || 'Notification';
          n.style.position = 'absolute';
          n.style.bottom = '10px';
          n.style.left = '50%';
          n.style.transform = 'translateX(-50%)';
          n.style.background = 'rgba(0,0,0,0.6)';
          n.style.padding = '6px 10px';
          n.style.borderRadius = '8px';
          n.style.pointerEvents = 'none';
          document.body.appendChild(n);
          setTimeout(() => n.remove(), 2000);
        }
      } catch {}
    });

    btnAction.addEventListener('click', () => {
      if (EBS_CONFIGURED) {
        sendAction('test', { source: 'overlay', at: Date.now() });
      }
    });
  }

  document.addEventListener('DOMContentLoaded', init);
})();
