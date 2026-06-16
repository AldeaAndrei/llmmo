const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

export class ApiError extends Error {
  constructor(status, message) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

let unauthorizedHandler = null

export function setUnauthorizedHandler(handler) {
  unauthorizedHandler = handler
}

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  const isAuthAttempt =
    path === '/auth/login' || path === '/auth/register'

  if (response.status === 401 && unauthorizedHandler && !isAuthAttempt) {
    unauthorizedHandler()
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw new ApiError(response.status, body.error ?? response.statusText)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

export const api = {
  register: (body) =>
    request('/auth/register', { method: 'POST', body: JSON.stringify(body) }),

  login: (body) =>
    request('/auth/login', { method: 'POST', body: JSON.stringify(body) }),

  logout: () => request('/auth/logout', { method: 'POST' }),

  getMe: () => request('/auth/me'),

  listAgents: () => request('/auth/agents'),

  createAgent: (body) =>
    request('/auth/agents', { method: 'POST', body: JSON.stringify(body) }),

  getAgent: (playerId) => request(`/auth/agents/${playerId}`),

  reissueAgentKey: (playerId) =>
    request(`/auth/agents/${playerId}/keys`, { method: 'POST' }),

  revokeAgentKey: (playerId) =>
    request(`/auth/agents/${playerId}/keys`, { method: 'DELETE' }),

  deleteAgent: (playerId) =>
    request(`/auth/agents/${playerId}`, { method: 'DELETE' }),

  getMap: () => request('/map'),

  getWorld: () => request('/world'),

  getMyCities: () => request('/cities/me'),

  getCityPublic: (cityId) => request(`/cities/${cityId}`),

  createAction: (body) =>
    request('/actions', { method: 'POST', body: JSON.stringify(body) }),

  getActions: (cityId) => request(`/actions?city_id=${cityId}`),

  getTroopCatalog: () => request('/catalog/troops'),

  getAttacks: (cityId) =>
    request(cityId ? `/attacks?city_id=${cityId}` : '/attacks'),

  getTroopMovements: (cityId) =>
    request(cityId ? `/attacks/movements?city_id=${cityId}` : '/attacks/movements'),

  previewAttack: (body) =>
    request('/attacks/preview', { method: 'POST', body: JSON.stringify(body) }),

  createAttack: (body) =>
    request('/attacks', { method: 'POST', body: JSON.stringify(body) }),

  getReports: () => request('/reports'),

  getReport: (reportId) => request(`/reports/${reportId}`),

  markReportRead: (reportId) =>
    request(`/reports/${reportId}/read`, { method: 'POST' }),
}
