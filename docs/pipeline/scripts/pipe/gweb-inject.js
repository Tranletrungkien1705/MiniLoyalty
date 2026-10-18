// Chen bang "Gemini Web (gweb)" vao /dashboard/usage va /dashboard/quota cua 9router. Du lieu: repos\gweb-stats.json (scripts\pipe\gweb-stats.ps1 ghi moi chu ky).
// Cai: scripts\pipe\apply-gweb-panel.ps1 (copy file nay vao 9router\app, them 1 dong require dau server.js + custom-server.js, sao luu .bak), roi khoi dong lai 9router.
const http = require("http"), fs = require("fs");
const STATS = "C:\\Users\\Public\\Pictures\\kientran_private\\repos\\gweb-stats.json";
const PANEL = `(function(){
  var ok=function(){return /^\\/dashboard\\/(usage|quota)\\/?$/.test(location.pathname)};
  function esc(s){return String(s==null?'':s).replace(/[&<>]/g,function(c){return {'&':'&amp;','<':'&lt;','>':'&gt;'}[c]})}
  function box(){var b=document.getElementById('gweb-panel');if(!b){b=document.createElement('div');b.id='gweb-panel';
    b.style.cssText='position:fixed;right:16px;bottom:16px;z-index:99999;max-width:560px;max-height:60vh;overflow:auto;background:#111827;color:#e5e7eb;border:1px solid #374151;border-radius:10px;padding:10px 12px;font:12px/1.4 system-ui,sans-serif;box-shadow:0 4px 20px rgba(0,0,0,.4)';
    document.body.appendChild(b);}return b}
  function render(d){var b=box();if(!ok()){b.style.display='none';return}b.style.display='block';
    var h='<div style="font-weight:600;font-size:13px;margin-bottom:6px">Gemini Web (gweb) <span style="font-weight:400;color:#9ca3af">cap nhat '+esc(d.updated)+'</span></div>';
    h+='<div style="margin-bottom:6px">Chrome: '+d.chrome.map(function(c){return 'cong '+c.port+(c.up?' <span style="color:#34d399">chay</span> ('+c.pages+' tab)':' <span style="color:#f87171">tat</span>')}).join(', ')+'</div>';
    h+='<table style="width:100%;border-collapse:collapse"><tr style="color:#9ca3af;text-align:left"><th>Du an</th><th>Slot</th><th>Dang lam</th><th>Hom nay OK/Bat dau/Loi/Quota</th><th>Da chot</th><th>Nghi</th></tr>';
    d.slots.forEach(function(s){var t=s.today;h+='<tr style="border-top:1px solid #374151"><td>'+esc(s.project)+'</td><td>'+esc(s.worker)+'</td><td>'+esc(s.current||'-')+'</td><td>'+t.ok+' / '+t.started+' / '+t.err+' / '+t.quota+'</td><td>'+s.done_total+'</td><td>'+esc(s.cooldown||'')+'</td></tr>'});
    b.innerHTML=h+'</table>'}
  function tick(){fetch('/__gweb/status',{cache:'no-store'}).then(function(r){return r.json()}).then(render).catch(function(){})}
  var last=null;function tick(){fetch('/__gweb/status',{cache:'no-store'}).then(function(r){return r.json()}).then(function(d){last=d;render(d)}).catch(function(){})}
  tick();setInterval(tick,15000);setInterval(function(){if(last)render(last)},1000);
})();`;
if (!global.__gwebInj) {
  global.__gwebInj = true;
  const orig = http.createServer.bind(http);
  http.createServer = (...a) => {
    const h = a.find((x) => typeof x === "function"); if (!h) return orig(...a);
    const rest = a.filter((x) => typeof x !== "function");
    const w = (req, res) => {
      try {
        const u = String(req.url || "").split("?")[0];
        if (u === "/__gweb/status") {
          let body = '{"updated":"chua co du lieu","chrome":[],"slots":[]}';
          try { body = fs.readFileSync(STATS, "utf8").replace(/^\uFEFF/, ""); } catch (e) {}
          res.writeHead(200, { "content-type": "application/json; charset=utf-8", "cache-control": "no-store" }); return res.end(body);
        }
        if (u === "/__gweb/panel.js") { res.writeHead(200, { "content-type": "application/javascript; charset=utf-8", "cache-control": "no-store" }); return res.end(PANEL); }
        if (req.method === "GET" && /^\/dashboard(\/.*)?$/.test(u) && !/\.(js|css|png|ico|svg|woff2?|json|map)$/.test(u) && !req.headers["rsc"] && !/_rsc=/.test(String(req.url)) && String(req.headers.accept || "").includes("text/html")) {   // moi trang dashboard (dieu huong SPA khong tai lai trang); bang tu an neu khong phai usage/quota
          delete req.headers["accept-encoding"];
          const ow = res.write.bind(res), oe = res.end.bind(res); let done = false; const tag = '<script src="/__gweb/panel.js"></script>';
          const fix = (c) => { if (done || c == null || typeof c === "function") return c; const s = (Buffer.isBuffer(c) || c instanceof Uint8Array) ? Buffer.from(c).toString("utf8") : String(c); const i = s.lastIndexOf("</body>"); if (i < 0) return c; done = true; return s.slice(0, i) + tag + s.slice(i); };
          // Next dat Content-Length/ETag cua trang goc (prerender) -> noi dung chen them bi cat. Bo hai header do, khong cache.
          const osh = res.setHeader.bind(res), owh = res.writeHead.bind(res);
          res.setHeader = (k, v) => { const l = String(k).toLowerCase(); if (l === "content-length" || l === "etag") return res; if (l === "cache-control") v = "no-store"; return osh(k, v); };
          res.writeHead = (code, ...a) => { for (const o of a) { if (o && typeof o === "object" && !Array.isArray(o)) { for (const k of Object.keys(o)) { const l = k.toLowerCase(); if (l === "content-length" || l === "etag") delete o[k]; else if (l === "cache-control") o[k] = "no-store"; } } } try { res.removeHeader("content-length"); res.removeHeader("etag"); } catch (e) {} return owh(code, ...a); };
          try { res.removeHeader("content-length"); res.removeHeader("etag"); } catch (e) {}
          res.write = (c, ...r) => ow(fix(c), ...r);
          res.end = (c, ...r) => { const x = fix(c); if (!done) { done = true; ow(tag); } return oe(x, ...r); };
        }
      } catch (e) {}
      return h(req, res);
    };
    return orig(...rest, w);
  };
}
