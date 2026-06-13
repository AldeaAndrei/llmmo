import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'

import { api } from '@/lib/api'

const WorldContext = createContext(null)

export function WorldProvider({ children }) {
  const [world, setWorld] = useState(null)
  const [loading, setLoading] = useState(true)
  const [now, setNow] = useState(Date.now())

  const refreshWorld = useCallback(async () => {
    try {
      const data = await api.getWorld()
      setWorld(data)
      return data
    } catch {
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    refreshWorld()
    const pollId = setInterval(refreshWorld, 1000)
    return () => clearInterval(pollId)
  }, [refreshWorld])

  useEffect(() => {
    const clockId = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(clockId)
  }, [])

  const secondsUntilNextTick = useMemo(() => {
    if (!world?.nextTickAt) {
      return null
    }

    const remainingMs = new Date(world.nextTickAt).getTime() - now
    return Math.max(0, Math.ceil(remainingMs / 1000))
  }, [world?.nextTickAt, now])

  const value = useMemo(
    () => ({
      world,
      loading,
      currentTick: world?.currentTick ?? 0,
      tickIntervalSeconds: world?.tickIntervalSeconds ?? 5,
      nextTickAt: world?.nextTickAt ?? null,
      secondsUntilNextTick,
      refreshWorld,
    }),
    [world, loading, secondsUntilNextTick, refreshWorld],
  )

  return (
    <WorldContext.Provider value={value}>{children}</WorldContext.Provider>
  )
}

export function useWorld() {
  const context = useContext(WorldContext)
  if (!context) {
    throw new Error('useWorld must be used within WorldProvider')
  }
  return context
}
