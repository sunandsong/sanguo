// 云函数 liubei —— 刘备英雄传后端（部署在人生清单环境，和群本玩的 jbs 并列、互不干扰）
//   POST /liubei/login { code? }              有 wx.login 的 code → openid；没有 → 发一个游客 id。返回 JWT
//   POST /liubei/load                         带 Authorization: Bearer <token>，返回三个档 [{ data, t } | null]
//   POST /liubei/save { slot, data, t }       slot 0..2，data 是存档 JSON 字符串，t 是存档时间（毫秒）
// 环境变量：WX_APPID / WX_APPSECRET / JWT_SECRET / CLOUDBASE_ENV_ID
const https = require('https');
const crypto = require('crypto');
const cloudbase = require('@cloudbase/node-sdk');
const { signJwt, verifyJwt } = require('./jwt');

const { WX_APPID, WX_APPSECRET, JWT_SECRET } = process.env;
const app = cloudbase.init({ env: process.env.CLOUDBASE_ENV_ID || cloudbase.SYMBOL_CURRENT_ENV });
const db = app.database();
const SAVES = 'liubei_saves';
const SLOTS = 3;

const reply = (statusCode, data) => ({
  statusCode,
  headers: { 'content-type': 'application/json; charset=utf-8' },
  body: JSON.stringify(data),
  isBase64Encoded: false,
});

function getJson(url) {
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      let buf = '';
      res.on('data', (c) => { buf += c; });
      res.on('end', () => { try { resolve(JSON.parse(buf)); } catch (e) { reject(e); } });
    }).on('error', reject);
  });
}

function parseBody(event) {
  if (!event.body) return {};
  const text = event.isBase64Encoded ? Buffer.from(event.body, 'base64').toString('utf8') : event.body;
  try { return JSON.parse(text) || {}; } catch (e) { return {}; }
}

let ensured = null;   // 集合不存在时写入会失败；冷启动补建一次，已存在就忽略
const ensure = () => ensured || (ensured = db.createCollection(SAVES).catch(() => {}));

async function readDoc(uid) {
  const r = await db.collection(SAVES).doc(uid).get();
  return (Array.isArray(r.data) ? r.data[0] : r.data) || null;
}

exports.main = async (event) => {
  if (!JWT_SECRET) return reply(500, { ok: false, msg: '服务未配置 JWT_SECRET' });
  if (String(event.httpMethod || '').toUpperCase() !== 'POST') return reply(405, { ok: false, msg: 'method not allowed' });
  const path = String(event.path || '').split('?')[0].replace(/\/+$/, '');
  const body = parseBody(event);

  if (path.endsWith('/login')) {
    let uid;
    if (body.code) {
      const r = await getJson(
        `https://api.weixin.qq.com/sns/jscode2session?appid=${WX_APPID}&secret=${WX_APPSECRET}` +
        `&js_code=${encodeURIComponent(body.code)}&grant_type=authorization_code`
      ).catch(() => null);
      if (!r || !r.openid) return reply(401, { ok: false, msg: '登录失败', errcode: r && r.errcode });
      uid = r.openid;
    } else {
      uid = 'guest-' + crypto.randomBytes(12).toString('hex');   // Mac 包 / 编辑器：游客，只认这台机器上存的 token
    }
    return reply(200, { ok: true, uid, token: signJwt({ sub: uid }, JWT_SECRET) });
  }

  const auth = (event.headers && (event.headers.authorization || event.headers.Authorization)) || '';
  const payload = verifyJwt(auth.replace(/^Bearer\s+/i, ''), JWT_SECRET);
  if (!payload || !payload.sub) return reply(401, { ok: false, msg: 'unauthorized' });
  const uid = payload.sub;
  await ensure();

  if (path.endsWith('/load')) {
    const doc = await readDoc(uid);
    const slots = [];
    for (let i = 0; i < SLOTS; i++) slots.push((doc && doc['s' + i]) || null);
    return reply(200, { ok: true, slots });
  }

  if (path.endsWith('/save')) {
    const slot = body.slot;
    if (!Number.isInteger(slot) || slot < 0 || slot >= SLOTS) return reply(400, { ok: false, msg: 'slot 不对' });
    if (typeof body.data !== 'string' || body.data.length > 60000) return reply(400, { ok: false, msg: 'data 不对' });
    const t = Number(body.t) || Date.now();
    const doc = (await readDoc(uid)) || {};
    delete doc._id;
    doc['s' + slot] = { data: body.data, t };
    await db.collection(SAVES).doc(uid).set(doc);
    return reply(200, { ok: true });
  }

  return reply(404, { ok: false, msg: 'not found' });
};
