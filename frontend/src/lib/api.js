const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api/v1'

export class ApiError extends Error {
  constructor(status, message) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

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
  getMap: () => request('/map'),

  getCities: (playerId) => request(`/cities?player_id=${playerId}`),

  getCity: (cityId, playerId) =>
    request(`/cities/${cityId}${playerId ? `?player_id=${playerId}` : ''}`),

  createPlayer: (body) =>
    request('/players', { method: 'POST', body: JSON.stringify(body) }),

  createAction: (body) =>
    request('/actions', { method: 'POST', body: JSON.stringify(body) }),

  getActions: (cityId) => request(`/actions?city_id=${cityId}`),
}
