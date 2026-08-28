const base = '/api/v1'
async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
    ...opts, body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  const text = await res.text(); const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}
export const api = {
  dashboard: () => req('/dashboard'),
  ranks: () => req('/ranks'),
  members: (q, rankId) => req(`/members?${q ? `q=${encodeURIComponent(q)}&` : ''}${rankId ? `rankId=${rankId}` : ''}`),
  member: (id) => req(`/members/${id}`),
  create: (b) => req('/members', { method: 'POST', body: b }),
  earn: (id, b) => req(`/members/${id}/earn`, { method: 'POST', body: b }),
  redeem: (id, rewardId) => req(`/members/${id}/redeem`, { method: 'POST', body: { rewardId } }),
  rewards: () => req('/rewards')
}
export const fmtMoney = (n) => (n ?? 0).toLocaleString('vi-VN') + 'đ'
export const fmtNum = (n) => (n ?? 0).toLocaleString('vi-VN')
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
