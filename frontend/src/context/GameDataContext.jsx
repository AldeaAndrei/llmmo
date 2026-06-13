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
import { useAuth } from '@/context/AuthContext'

const GameDataContext = createContext(null)

export function GameDataProvider({ children }) {
  const { isAuthenticated, playerId } = useAuth()
  const [mapCities, setMapCities] = useState([])
  const [cities, setCities] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionRevision, setActionRevision] = useState(0)

  const primaryCity = cities[0] ?? null

  const refreshMap = useCallback(async () => {
    const data = await api.getMap()
    setMapCities(data)
    return data
  }, [])

  const refreshCities = useCallback(async () => {
    if (!isAuthenticated) {
      setCities([])
      return []
    }

    const data = await api.getMyCities()
    setCities(data)
    return data
  }, [isAuthenticated])

  const loadGame = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      await refreshMap()
      if (isAuthenticated) {
        await refreshCities()
      } else {
        setCities([])
      }
    } catch (err) {
      setError(err.message ?? 'Failed to load game data')
      toast.error(err.message ?? 'Failed to load game data')
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated, refreshCities, refreshMap])

  useEffect(() => {
    loadGame()
  }, [loadGame, isAuthenticated, playerId])

  useEffect(() => {
    if (!isAuthenticated) return

    const refresh = () => {
      refreshCities()
      refreshMap()
    }

    window.addEventListener('focus', refresh)
    return () => window.removeEventListener('focus', refresh)
  }, [isAuthenticated, refreshCities, refreshMap])

  const submitAction = useCallback(
    async (type, payload, cityId = primaryCity?.id) => {
      if (!isAuthenticated || !cityId) {
        toast.error('Log in to queue actions')
        return null
      }

      try {
        const action = await api.createAction({
          cityId,
          type,
          payload,
        })
        await refreshCities()
        setActionRevision((revision) => revision + 1)
        toast.success(`${type} action queued`)
        return action
      } catch (err) {
        toast.error(err.message ?? 'Action failed')
        throw err
      }
    },
    [isAuthenticated, primaryCity, refreshCities],
  )

  const value = useMemo(
    () => ({
      mapCities,
      playerId,
      cities,
      primaryCity,
      loading,
      error,
      isAuthenticated,
      actionRevision,
      refreshMap,
      refreshCities,
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
      isAuthenticated,
      actionRevision,
      refreshMap,
      refreshCities,
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
