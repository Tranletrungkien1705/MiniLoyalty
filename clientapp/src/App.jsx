import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtNum, fmtDate, fmtDateTime } from './api'

function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 680 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>{children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }
function RankChip({ name, color }) { return name ? <span className="badge" style={{ background: color || '#94a3b8' }}>{name}</span> : <span className="muted">—</span> }

function Layout() {
  return (
    <>
      <nav className="nav"><span className="brand">💳 MiniLoyalty</span>
        <NavLink to="/" end>Tổng quan</NavLink><NavLink to="/members">Hội viên</NavLink>
        <NavLink to="/rewards">Quà đổi</NavLink><NavLink to="/ranks">Hạng thẻ</NavLink></nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  const max = Math.max(1, ...d.byRank.map(x => x.count))
  return (
    <>
      <h1>Tổng quan loyalty {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis" style={{ marginBottom: 18 }}>
        <div className="kpi"><div className="v">{d.members}</div><div className="l">Hội viên</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--success)' }}>{fmtNum(d.activePoints)}</div><div className="l">Điểm khả dụng</div></div>
        <div className="kpi"><div className="v">{fmtNum(d.lifetimeIssued)}</div><div className="l">Điểm tích lũy trọn đời</div></div>
      </div>
      <div className="card funnel"><h2>Hội viên theo hạng thẻ</h2>
        {d.byRank.map((x, i) => (<div className="bar" key={i}><div className="lbl">{x.rank}</div>
          <div className="track"><div className="fill" style={{ width: `${(x.count / max) * 100}%`, background: x.color }} /></div><div className="n">{x.count}</div></div>))}
      </div>
    </>
  )
}

function Members() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [ranks, setRanks] = useState([]); const [rankId, setRankId] = useState('')
  const [open, setOpen] = useState(null); const [show, setShow] = useState(false)
  const load = () => api.members(q, rankId || null).then(r => setRows(r.data))
  useEffect(() => { load() }, [rankId])
  useEffect(() => { api.ranks().then(r => setRanks(r.data)) }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Hội viên</h1><div className="sp" />
        <select style={{ maxWidth: 150 }} value={rankId} onChange={e => setRankId(e.target.value)}><option value="">— Hạng —</option>{ranks.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}</select>
        <input style={{ maxWidth: 180 }} placeholder="Tìm tên/SĐT…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Hội viên</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Tên</th><th>SĐT</th><th>Hạng</th><th className="right">Điểm KD</th><th className="right">Trọn đời</th></tr></thead>
          <tbody>{rows.map(m => (<tr key={m.id} style={{ cursor: 'pointer' }} onClick={() => setOpen(m.id)}>
            <td>{m.code}</td><td>{m.name}</td><td>{m.phone || '—'}</td><td><RankChip name={m.rank} color={m.rankColor} /></td>
            <td className="right"><b>{fmtNum(m.points)}</b></td><td className="right muted">{fmtNum(m.lifetimePoints)}</td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có hội viên.</td></tr>}</tbody></table>
      </div>
      {open && <MemberDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
      {show && <MemberForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function MemberDetail({ id, onClose, onChanged }) {
  const [m, setM] = useState(null); const [rewards, setRewards] = useState([]); const [msg, setMsg] = useState(null)
  const [amount, setAmount] = useState(''); const [points, setPoints] = useState('')
  const load = () => api.member(id).then(r => setM(r.data))
  useEffect(() => { load(); api.rewards().then(r => setRewards(r.data.filter(x => x.isActive))) }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const earnPurchase = async () => { try { const r = await api.earn(id, { amount: Number(amount) }); flash(true, `+${r.data.points} điểm (số dư ${r.data.balance})`); setAmount(''); load(); onChanged() } catch (e) { flash(false, e.message) } }
  const earnManual = async () => { try { const r = await api.earn(id, { points: Number(points), note: 'Điều chỉnh thủ công' }); flash(true, `${r.data.points >= 0 ? '+' : ''}${r.data.points} điểm`); setPoints(''); load(); onChanged() } catch (e) { flash(false, e.message) } }
  const redeem = async (rid) => { try { const r = await api.redeem(id, rid); flash(true, r.data.msg); load(); onChanged() } catch (e) { flash(false, e.message) } }
  if (!m) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`${m.name} (${m.code})`} onClose={onClose} wide>
      <Flash msg={msg} />
      <div className="row" style={{ marginBottom: 8 }}><RankChip name={m.rank} color={m.rankColor} />
        <span className="pill" style={{ flex: 'none' }}>Điểm: {fmtNum(m.points)}</span>
        <span className="pill" style={{ flex: 'none' }}>Trọn đời: {fmtNum(m.lifetimePoints)}</span>
        {m.discount > 0 && <span className="pill" style={{ flex: 'none' }}>CK {m.discount}%</span>}</div>
      <dl className="dl"><dt>SĐT</dt><dd>{m.phone || '—'}</dd><dt>Email</dt><dd>{m.email || '—'}</dd><dt>Ngày sinh</dt><dd>{fmtDate(m.dob)}</dd><dt>Tham gia</dt><dd>{fmtDate(m.joinedAt)}</dd></dl>
      <div className="card" style={{ background: '#f8fafc' }}>
        <div className="section-t">Tích điểm</div>
        <div className="row"><Field label="Từ giá trị mua (1đ = 1.000₫ → tự quy đổi)"><input type="number" value={amount} onChange={e => setAmount(e.target.value)} /></Field>
          <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn sm" onClick={earnPurchase} disabled={!amount}>Tích từ mua hàng</button></div>
          <Field label="Điểm điều chỉnh"><input type="number" value={points} onChange={e => setPoints(e.target.value)} /></Field>
          <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn ghost sm" onClick={earnManual} disabled={!points}>Điều chỉnh</button></div></div>
      </div>
      <div className="section-t">Đổi quà</div>
      <table><tbody>{rewards.map(rw => (<tr key={rw.id}><td>{rw.name}</td><td className="muted">{fmtNum(rw.pointCost)} điểm · còn {rw.stock}</td>
        <td className="right"><button className="btn sm" style={{ flex: 'none' }} disabled={m.points < rw.pointCost || rw.stock <= 0} onClick={() => redeem(rw.id)}>Đổi</button></td></tr>))}</tbody></table>
      <div className="section-t">Lịch sử điểm</div>
      <div style={{ maxHeight: 200, overflow: 'auto' }}>
        <table><tbody>{m.transactions.map((t, i) => <tr key={i}><td>{t.type}</td><td className="muted">{t.note || t.refNo || ''}</td>
          <td className="right" style={{ color: t.points >= 0 ? 'var(--success)' : 'var(--danger)' }}>{t.points >= 0 ? '+' : ''}{fmtNum(t.points)}</td><td className="right muted">{fmtNum(t.balanceAfter)}</td><td className="muted" style={{ fontSize: 12 }}>{fmtDateTime(t.createdAt)}</td></tr>)}</tbody></table>
      </div>
    </Modal>
  )
}

function MemberForm({ onClose, onSaved }) {
  const [f, setF] = useState({ name: '', phone: '', email: '', dob: '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => { try { if (!f.name) { setErr('Cần tên'); return } await api.create({ ...f, dob: f.dob || null }); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title="Thêm hội viên" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tên *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
        <Field label="SĐT"><input value={f.phone} onChange={e => up('phone', e.target.value)} /></Field></div>
      <div className="row"><Field label="Email"><input value={f.email} onChange={e => up('email', e.target.value)} /></Field>
        <Field label="Ngày sinh"><input type="date" value={f.dob} onChange={e => up('dob', e.target.value)} /></Field></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function Rewards() {
  const [rows, setRows] = useState([])
  useEffect(() => { api.rewards().then(r => setRows(r.data)) }, [])
  return (
    <>
      <h1>Quà đổi điểm</h1>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Quà</th><th>Mô tả</th><th className="right">Điểm đổi</th><th className="right">Còn</th><th>Trạng thái</th></tr></thead>
          <tbody>{rows.map(r => <tr key={r.id}><td>{r.name}</td><td className="muted">{r.description || '—'}</td><td className="right">{fmtNum(r.pointCost)}</td><td className="right">{r.stock}</td>
            <td><span className={`badge ${r.isActive ? 'success' : 'dark'}`}>{r.isActive ? 'Đang mở' : 'Ngừng'}</span></td></tr>)}</tbody></table>
      </div>
    </>
  )
}

function Ranks() {
  const [rows, setRows] = useState([])
  useEffect(() => { api.ranks().then(r => setRows(r.data)) }, [])
  return (
    <>
      <h1>Hạng thẻ thành viên</h1>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Hạng</th><th className="right">Điểm tối thiểu (trọn đời)</th><th className="right">Chiết khấu</th></tr></thead>
          <tbody>{rows.map(r => <tr key={r.id}><td><span className="badge" style={{ background: r.colorHex }}>{r.name}</span></td>
            <td className="right">{fmtNum(r.minLifetimePoints)}</td><td className="right">{r.discountPercent}%</td></tr>)}</tbody></table>
      </div>
    </>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="members" element={<Members />} />
        <Route path="rewards" element={<Rewards />} />
        <Route path="ranks" element={<Ranks />} />
      </Route>
    </Routes>
  )
}
