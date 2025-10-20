/* global Twitch */
(function () {
  const statusEl = document.getElementById('status');
  const logEl = document.getElementById('log');
  const btnTest = document.getElementById('btn-test');

  const STATE = {
    token: null,
    channelId: null,
    opaqueUserId: null,
    userId: null,
    role: null,
  };

  const EXT_EBS_BASE_URL = 'https://your-ebs.example.com'; // TODO: replace with your EBS base URL
  const EBS_CONFIGURED = typeof EXT_EBS_BASE_URL === 'string' && !/your-ebs\.example\.com/i.test(EXT_EBS_BASE_URL);

  function log(msg, obj) {
    const time = new Date().toISOString();
    const line = obj ? `${time} ${msg} \n${JSON.stringify(obj, null, 2)}\n` : `${time} ${msg}`;
    console.log(msg, obj || '');
    logEl.textContent = `${line}\n${logEl.textContent}`;
  }

  function parseJwtPayload(token) {
    try {
      const base64 = token.split('.')[1];
      const json = atob(base64.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decodeURIComponent(escape(json)));
    } catch (e) {
      return null;
    }
  }

  async function sendAction(action, payload = {}) {
    if (!STATE.token || !EBS_CONFIGURED) return;
    try {
      const res = await fetch(`${EXT_EBS_BASE_URL}/ext/action`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${STATE.token}`,
        },
        body: JSON.stringify({ action, payload }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.message || `HTTP ${res.status}`);
      log(`Action '${action}' sent successfully`, data);
    } catch (err) {
      log(`Error sending action '${action}': ${err.message}`);
    }
  }

  function onAuthorized(auth) {
    STATE.token = auth.token;
    STATE.channelId = auth.channelId;
    const payload = parseJwtPayload(auth.token) || {};
    STATE.opaqueUserId = payload.opaque_user_id || null;
    STATE.userId = payload.user_id || null;
    STATE.role = payload.role || null;

    statusEl.textContent = `Authorized as ${STATE.userId ? `user ${STATE.userId}` : 'anonymous'} (role: ${STATE.role || 'viewer'})`;
    if (!EBS_CONFIGURED) {
      statusEl.textContent += ' • EBS not configured';
    }
    btnTest.disabled = !EBS_CONFIGURED;
    log('Authorized payload', { channelId: STATE.channelId, role: STATE.role, userId: STATE.userId, opaqueUserId: STATE.opaqueUserId });
  }

  function onContext(context, changed) {
    if (changed.includes('theme')) {
      document.body.dataset.theme = context.theme || 'light';
    }
  }

  function onError(err) {
    log(`Twitch.ext error: ${err}`);
  }

  function init() {
    if (!window.Twitch || !window.Twitch.ext) {
      statusEl.textContent = 'Not running inside Twitch Extension iframe';
      return;
    }

    Twitch.ext.onAuthorized(onAuthorized);
    Twitch.ext.onContext(onContext);
    Twitch.ext.onError(onError);

    // Listen for broadcast messages for dev visibility
    Twitch.ext.listen('broadcast', (topic, message) => {
      try { log('broadcast', JSON.parse(message)); } catch { log('broadcast', message); }
    });

    if (!EBS_CONFIGURED) {
      log('EBS not configured; set EXT_EBS_BASE_URL in index.js to enable actions.');
    } else {
      btnTest.addEventListener('click', () => {
        sendAction('test', { at: Date.now() });
      });
    }
  }

  document.addEventListener('DOMContentLoaded', init);
})();

