import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '@/lib/api'
import { useGameData } from '@/context/GameDataContext'

export function useCityActions(cityId) {
  const { actionRevision } = useGameData()
  const [actions, setActions] = useState([])
  const [error, setError] = useState(null)
  const [initialLoading, setInitialLoading] = useState(false)
  const hasLoadedRef = useRef(false)
  const prevCityIdRef = useRef(cityId)
  const [manualRevision, setManualRevision] = useState(0)

  const refresh = useCallback(() => {
    setManualRevision((revision) => revision + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    if (!cityId) {
      setActions([])
      setError(null)
      setInitialLoading(false)
      hasLoadedRef.current = false
      prevCityIdRef.current = cityId
      return
    }

    if (prevCityIdRef.current !== cityId) {
      setActions([])
      setError(null)
      hasLoadedRef.current = false
      prevCityIdRef.current = cityId
    }

    const isInitialLoad = !hasLoadedRef.current

    if (isInitialLoad) {
      setInitialLoading(true)
    }

    async function load() {
      try {
        const data = await api.getActions(cityId)
        if (cancelled) return
        setActions(data)
        setError(null)
        hasLoadedRef.current = true
      } catch (err) {
        if (cancelled) return
        setError(err.message ?? 'Failed to load actions')
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
  }, [cityId, actionRevision, manualRevision])

  return { actions, loading: initialLoading, error, refresh }
}
