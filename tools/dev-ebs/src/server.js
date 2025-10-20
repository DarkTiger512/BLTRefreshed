import 'dotenv/config';
import express from 'express';
import cors from 'cors';
import rateLimit from 'express-rate-limit';
import crypto from 'crypto';
import jwt from 'jsonwebtoken';
import fetch from 'node-fetch';
import { WebSocketServer } from 'ws';
import fs from 'fs';
import path from 'path';

const PORT = process.env.PORT || 8088;
const EXT_CLIENT_ID = process.env.EXT_CLIENT_ID;
const EXT_SECRET = process.env.EXT_SECRET; // base64
const TWITCH_CLIENT_ID = process.env.TWITCH_CLIENT_ID || process.env.EXT_CLIENT_ID;
const TWITCH_CLIENT_SECRET = process.env.TWITCH_CLIENT_SECRET;
const MOD_SHARED_SECRET = process.env.MOD_SHARED_SECRET;
const MOD_ALLOWED_ORIGIN = process.env.MOD_ALLOWED_ORIGIN ?? '*';

if (!EXT_CLIENT_ID || !EXT_SECRET) {
  console.warn('[WARN] EXT_CLIENT_ID/EXT_SECRET not set; /ext/action will reject requests');
}
if (!TWITCH_CLIENT_ID || !TWITCH_CLIENT_SECRET) {
  console.warn('[WARN] TWITCH_CLIENT_ID/TWITCH_CLIENT_SECRET not set; Extensions PubSub will be disabled');
}

const app = express();
// Configure CORS: allow true when wildcard
app.use(cors({ origin: MOD_ALLOWED_ORIGIN === '*' ? true : MOD_ALLOWED_ORIGIN, credentials: false }));
app.use(express.json({ limit: '256kb' }));

// Rate limiter for actions: window 10s, max 10 per key (user or IP)
const actionLimiter = rateLimit({
  windowMs: 10000,
  max: 10,
  standardHeaders: true,
  legacyHeaders: false,
  keyGenerator: (req) => {
    try {
      const auth = req.headers.authorization || '';
      const token = auth.startsWith('Bearer ') ? auth.slice(7) : '';
      if (token && EXT_SECRET) {
        const payload = verifyExtensionJwt(token);
        if (payload) return payload.opaque_user_id || payload.user_id || req.ip;
      }
    } catch (e) {
      // ignore and fallback to IP
    }
    return req.ip;
  },
  handler: (req, res) => res.status(429).json({ error: 'rate_limited' })
});

// In-memory channel routing for dev
const modSocketsByChannel = new Map(); // channel_id -> Set<WebSocket>
const CONFIG_DIR = path.join(process.cwd(), 'data');
fs.mkdirSync(CONFIG_DIR, { recursive: true });

function configPath(channelId) {
  return path.join(CONFIG_DIR, `${channelId}.json`);
}
function readConfig(channelId) {
  try { return JSON.parse(fs.readFileSync(configPath(channelId), 'utf8')); } catch { return {}; }
}
function writeConfig(channelId, cfg) {
  fs.writeFileSync(configPath(channelId), JSON.stringify(cfg, null, 2));
}

function verifyExtensionJwt(token) {
  try {
    const payload = jwt.verify(token, Buffer.from(EXT_SECRET, 'base64'), {
      algorithms: ['HS256'],
      audience: EXT_CLIENT_ID,
    });
    return payload; // { channel_id, user_id?, opaque_user_id, role, ... }
  } catch (e) {
    return null;
  }
}

let appAccessToken = null;
let appAccessTokenExp = 0;
async function getAppAccessToken() {
  const now = Math.floor(Date.now() / 1000);
  if (appAccessToken && appAccessTokenExp - 60 > now) return appAccessToken;
  const url = `https://id.twitch.tv/oauth2/token?client_id=${encodeURIComponent(TWITCH_CLIENT_ID)}&client_secret=${encodeURIComponent(TWITCH_CLIENT_SECRET)}&grant_type=client_credentials`;
  const res = await fetch(url, { method: 'POST' });
  if (!res.ok) throw new Error(`Token HTTP ${res.status}`);
  const data = await res.json();
  appAccessToken = data.access_token;
  appAccessTokenExp = now + (data.expires_in || 3600);
  return appAccessToken;
}

async function publishToOverlay(channelId, messageObj) {
  if (!TWITCH_CLIENT_ID || !TWITCH_CLIENT_SECRET) return { ok: false, reason: 'pubsub_disabled' };
  const token = await getAppAccessToken();
  const res = await fetch('https://api.twitch.tv/helix/extensions/pubsub', {
    method: 'POST',
    headers: {
      'Client-ID': TWITCH_CLIENT_ID,
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      broadcaster_id: String(channelId),
      message: JSON.stringify(messageObj),
      target: ['broadcast'],
    }),
  });
  if (!res.ok) {
    const txt = await res.text();
    console.warn('[PubSub] error', res.status, txt);
    return { ok: false, status: res.status };
  }
  return { ok: true };
}

app.get('/health', (_req, res) => res.json({ ok: true }));

// Get per-channel config (any role may read)
app.get('/ext/config', (req, res) => {
  const auth = req.headers.authorization || '';
  const token = auth.startsWith('Bearer ') ? auth.slice(7) : '';
  if (!EXT_SECRET || !token) return res.status(401).json({ error: 'unauthorized' });
  const payload = verifyExtensionJwt(token);
  if (!payload) return res.status(401).json({ error: 'invalid_token' });
  const channelId = String(payload.channel_id);
  return res.json({ ok: true, config: readConfig(channelId) });
});

// Update per-channel config (broadcaster only)
app.post('/ext/config', (req, res) => {
  const auth = req.headers.authorization || '';
  const token = auth.startsWith('Bearer ') ? auth.slice(7) : '';
  if (!EXT_SECRET || !token) return res.status(401).json({ error: 'unauthorized' });
  const payload = verifyExtensionJwt(token);
  if (!payload) return res.status(401).json({ error: 'invalid_token' });
  if (payload.role !== 'broadcaster') return res.status(403).json({ error: 'forbidden' });
  const channelId = String(payload.channel_id);
  const cfg = (req.body && req.body.config) || {};
  writeConfig(channelId, cfg);
  // Broadcast config saved toast
  (async () => {
    try { await publishToOverlay(channelId, { type: 'toast', text: 'Config saved' }); } catch (e) { console.error('publish config toast error', e); }
  })();
  return res.json({ ok: true });
});

app.post('/ext/action', actionLimiter, async (req, res) => {
  try {
    const auth = req.headers.authorization || '';
    const token = auth.startsWith('Bearer ') ? auth.slice(7) : '';
    if (!EXT_SECRET || !token) return res.status(401).json({ error: 'unauthorized' });
    const payload = verifyExtensionJwt(token);
    if (!payload) return res.status(401).json({ error: 'invalid_token' });

    const { channel_id: channelId, role, user_id: userId, opaque_user_id: opaqueUserId } = payload;
    const { action, payload: bodyPayload } = req.body || {};
    if (!action) return res.status(400).json({ error: 'missing_action' });

    // Forward to connected mod clients for this channel
    const sockets = modSocketsByChannel.get(String(channelId));
    if (sockets && sockets.size) {
      const msg = JSON.stringify({ type: 'viewer_action', action, payload: bodyPayload, userId, opaqueUserId, role });
      for (const ws of sockets) try { ws.send(msg); } catch {}
    }

    // Dev: on 'test' action, publish a simple toast to overlay via PubSub
    if (action === 'test') {
      await publishToOverlay(String(channelId), { type: 'toast', text: 'Test action received' });
    }

    return res.json({ ok: true });
  } catch (e) {
    console.error(JSON.stringify({ evt: '/ext/action error', message: e?.message, stack: e?.stack }));
    return res.status(500).json({ error: 'server_error' });
  }
});

// Simple dev endpoint to push overlay state
app.post('/dev/broadcast/:channelId', async (req, res) => {
  try {
    const { channelId } = req.params;
    const { message } = req.body || {};
    const r = await publishToOverlay(String(channelId), message || { type: 'toast', text: 'hello from dev' });
    res.json(r);
  } catch (e) {
    res.status(500).json({ error: 'server_error' });
  }
});

const server = app.listen(PORT, () => console.log(`[EBS] listening on :${PORT}`));

// WebSocket server for the mod
const wss = new WebSocketServer({ noServer: true });
server.on('upgrade', (req, socket, head) => {
  const url = new URL(req.url, 'http://localhost');
  if (url.pathname !== '/mod') return socket.destroy();
  const channelId = url.searchParams.get('channel_id');
  if (!channelId) return socket.destroy();

  // Verify token query param when MOD_SHARED_SECRET is configured
  const token = url.searchParams.get('token');
  if (MOD_SHARED_SECRET) {
    if (!token) return socket.destroy();
    try {
      const expected = crypto.createHmac('sha256', MOD_SHARED_SECRET).update(String(channelId) + '.' + MOD_SHARED_SECRET).digest('hex');
      if (token !== expected) {
        console.warn(JSON.stringify({ evt: 'ws_auth_failure', channelId, reason: 'invalid_token' }));
        return socket.destroy();
      }
    } catch (e) {
      console.error('ws auth verify error', e);
      return socket.destroy();
    }
  }

  wss.handleUpgrade(req, socket, head, (ws) => {
    wss.emit('connection', ws, req, { channelId });
  });
});

wss.on('connection', (ws, _req, { channelId }) => {
  const key = String(channelId);
  let set = modSocketsByChannel.get(key);
  if (!set) modSocketsByChannel.set(key, (set = new Set()));
  set.add(ws);
  ws.send(JSON.stringify({ type: 'hello', channelId: key }));
  ws.on('close', () => set.delete(ws));
  ws.on('message', async (raw) => {
    try {
      const msg = JSON.parse(raw.toString());
      if (msg && msg.type === 'overlay_state') {
        // Map overlay_state to PubSub broadcast
        await publishToOverlay(key, { type: 'overlay_state', state: msg.state || 'update' });
      }
    } catch {}
  });
});
