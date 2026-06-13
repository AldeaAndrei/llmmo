import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { toast } from 'sonner'

import { api } from '@/lib/api'
import { findFreeTile } from '@/lib/map'

const STORAGE_PLAYER_ID = 'llmmo_player_id'

const GameDataContext = createContext(null)

function readStoredPlayerId() {
  const params = new URLSearchParams(window.location.search)
  const urlPlayerId = params.get('playerId')
  if (urlPlayerId) {
    localStorage.setItem(STORAGE_PLAYER_ID, urlPlayerId)
    return urlPlayerId
  }

  return localStorage.getItem(STORAGE_PLAYER_ID)
}

export function GameDataProvider({ children }) {
  const [mapCities, setMapCities] = useState([])
  const [playerId, setPlayerId] = useState(readStoredPlayerId)
  const [cities, setCities] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionRevision, setActionRevision] = useState(0)

  const primaryCity = cities[0] ?? null
  const needsJoin = !playerId || cities.length === 0

  const refreshMap = useCallback(async () => {
    const data = await api.getMap()
    setMapCities(data)
    return data
  }, [])

  const refreshCities = useCallback(async (id) => {
    const data = await api.getCities(id)
    setCities(data)
    return data
  }, [])

  const loadGame = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      await refreshMap()

      const storedId = readStoredPlayerId()
      if (!storedId) {
        setPlayerId(null)
        setCities([])
        return
      }

      setPlayerId(storedId)
      const playerCities = await refreshCities(storedId)
      if (playerCities.length === 0) {
        localStorage.removeItem(STORAGE_PLAYER_ID)
        setPlayerId(null)
      }
    } catch (err) {
      setError(err.message ?? 'Failed to load game data')
      toast.error(err.message ?? 'Failed to load game data')
    } finally {
      setLoading(false)
    }
  }, [refreshCities, refreshMap])

  useEffect(() => {
    loadGame()
  }, [loadGame])

  useEffect(() => {
    if (!playerId) return

    const refresh = () => {
      refreshCities(playerId)
      refreshMap()
    }

    window.addEventListener('focus', refresh)
    return () => window.removeEventListener('focus', refresh)
  }, [playerId, refreshCities, refreshMap])

  const joinGame = useCallback(
    async (name, playerType = 'human') => {
      setLoading(true)
      setError(null)

      try {
        const map = mapCities.length > 0 ? mapCities : await refreshMap()
        const { x, y } = findFreeTile(map)
        const created = await api.createPlayer({ name, playerType, x, y })

        localStorage.setItem(STORAGE_PLAYER_ID, created.playerId)
        setPlayerId(created.playerId)
        await refreshMap()
        await refreshCities(created.playerId)
        toast.success(`Welcome, ${created.name}`)
      } catch (err) {
        setError(err.message ?? 'Failed to join')
        toast.error(err.message ?? 'Failed to join')
        throw err
      } finally {
        setLoading(false)
      }
    },
    [mapCities, refreshCities, refreshMap],
  )

  const submitAction = useCallback(
    async (type, payload, cityId = primaryCity?.id) => {
      if (!playerId || !cityId) {
        toast.error('No city selected')
        return null
      }

      try {
        const action = await api.createAction({
          playerId,
          cityId,
          type,
          payload,
        })
        await refreshCities(playerId)
        setActionRevision((revision) => revision + 1)
        toast.success(`${type} action queued`)
        return action
      } catch (err) {
        toast.error(err.message ?? 'Action failed')
        throw err
      }
    },
    [playerId, primaryCity, refreshCities],
  )

  const value = useMemo(
    () => ({
      mapCities,
      playerId,
      cities,
      primaryCity,
      loading,
      error,
      needsJoin,
      actionRevision,
      refreshMap,
      refreshCities,
      joinGame,
      submitAction,
      reload: loadGame,
    }),
    [
      mapCities,
      playerId,
      cities,
      primaryCity,
      loading,
      error,
      needsJoin,
      actionRevision,
      refreshMap,
      refreshCities,
      joinGame,
      submitAction,
      loadGame,
    ],
  )

  return (
    <GameDataContext.Provider value={value}>{children}</GameDataContext.Provider>
  )
}

export function useGameData() {
  const context = useContext(GameDataContext)
  if (!context) {
    throw new Error('useGameData must be used within GameDataProvider')
  }
  return context
}
