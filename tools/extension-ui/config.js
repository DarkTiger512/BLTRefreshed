/* global Twitch */
(function () {
  const roleEl = document.getElementById('role');
  const statusEl = document.getElementById('status');
  const form = document.getElementById('cfgForm');
  const saveBtn = document.getElementById('saveBtn');

  const inputs = {
    theme: document.getElementById('theme'),
    showToasts: document.getElementById('showToasts'),
    primaryColor: document.getElementById('primaryColor'),
  };

  const STATE = { token: null, role: null };
  const EXT_EBS_BASE_URL = 'https://your-ebs.example.com'; // replace with your EBS
  const EBS_CONFIGURED = typeof EXT_EBS_BASE_URL === 'string' && !/your-ebs\.example\.com/i.test(EXT_EBS_BASE_URL);

  function setStatus(text) { statusEl.textContent = text; }
  function ensureBroadcaster() {
    if (STATE.role !== 'broadcaster') {
      setStatus('Broadcaster role required to save.');
      saveBtn.disabled = true;
      return false;
    }
    saveBtn.disabled = !EBS_CONFIGURED;
    return true;
  }

  function loadIntoForm(cfg) {
    if (!cfg) return;
    if (cfg.theme) inputs.theme.value = cfg.theme;
    if (typeof cfg.showToasts === 'boolean') inputs.showToasts.checked = cfg.showToasts;
    if (cfg.primaryColor) inputs.primaryColor.value = cfg.primaryColor;
  }

  async function fetchConfig() {
    if (!EBS_CONFIGURED || !STATE.token) return;
    try {
      const res = await fetch(`${EXT_EBS_BASE_URL}/ext/config`, {
        headers: { Authorization: `Bearer ${STATE.token}` },
      });
      const data = await res.json();
      if (res.ok && data && data.config) loadIntoForm(data.config);
    } catch {}
  }

  async function saveConfig(cfg) {
    const res = await fetch(`${EXT_EBS_BASE_URL}/ext/config`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${STATE.token}`,
      },
      body: JSON.stringify({ config: cfg }),
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
  }

  function onAuthorized(auth) {
    STATE.token = auth.token;
    try {
      const payload = JSON.parse(atob(auth.token.split('.')[1] || '')) || {};
      STATE.role = payload.role || null;
    } catch {}
    roleEl.textContent = `Role: ${STATE.role || 'viewer'}`;
    ensureBroadcaster();
    fetchConfig();
  }

  function onSubmit(e) {
    e.preventDefault();
    if (!ensureBroadcaster()) return;
    const cfg = {
      theme: inputs.theme.value,
      showToasts: inputs.showToasts.checked,
      primaryColor: inputs.primaryColor.value,
    };
    setStatus('Saving…');
    saveConfig(cfg)
      .then(() => setStatus('Saved'))
      .catch((err) => setStatus(`Error: ${err.message}`));
  }

  function init() {
    if (!window.Twitch || !window.Twitch.ext) {
      roleEl.textContent = 'Not in Twitch iframe';
      return;
    }
    Twitch.ext.onAuthorized(onAuthorized);
    form.addEventListener('submit', onSubmit);
    if (!EBS_CONFIGURED) setStatus('EBS not configured');
  }

  document.addEventListener('DOMContentLoaded', init);
})();

