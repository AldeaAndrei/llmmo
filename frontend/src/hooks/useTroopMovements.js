import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '@/lib/api'
import { useGameData } from '@/context/GameDataContext'
import { useWorld } from '@/context/WorldContext'

const EMPTY_MOVEMENTS = { outgoing: [], incoming: [] }

export function useTroopMovements(cityId) {
  const { actionRevision } = useGameData()
  const { currentTick } = useWorld()
  const [movements, setMovements] = useState(EMPTY_MOVEMENTS)
  const [initialLoading, setInitialLoading] = useState(false)
  const [hasLoaded, setHasLoaded] = useState(false)
  const hasLoadedRef = useRef(false)
  const prevCityIdRef = useRef(cityId)
  const [manualRevision, setManualRevision] = useState(0)

  const refresh = useCallback(() => {
    setManualRevision((revision) => revision + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    if (!cityId) {
      setMovements(EMPTY_MOVEMENTS)
      setInitialLoading(false)
      setHasLoaded(false)
      hasLoadedRef.current = false
      prevCityIdRef.current = cityId
      return
    }

    if (prevCityIdRef.current !== cityId) {
      setMovements(EMPTY_MOVEMENTS)
      setHasLoaded(false)
      hasLoadedRef.current = false
      prevCityIdRef.current = cityId
    }

    const isInitialLoad = !hasLoadedRef.current

    if (isInitialLoad) {
      setInitialLoading(true)
    }

    async function load() {
      try {
        const data = await api.getTroopMovements(cityId)
        if (cancelled) return
        setMovements({
          outgoing: data.outgoing ?? [],
          incoming: data.incoming ?? [],
        })
        hasLoadedRef.current = true
        setHasLoaded(true)
      } catch {
        if (cancelled || hasLoadedRef.current) return
        setMovements(EMPTY_MOVEMENTS)
      } finally {
        if (!cancelled && isInitialLoad) {
          setInitialLoading(false)
        }
      }
    }

    load()

    return () => {
      cancelled = true
    }
  }, [cityId, actionRevision, manualRevision, currentTick])

  return { movements, loading: initialLoading, hasLoaded, refresh }
}
