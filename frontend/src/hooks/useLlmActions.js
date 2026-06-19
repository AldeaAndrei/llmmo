import { useCallback, useEffect, useRef, useState } from 'react'

import { api } from '@/lib/api'
import { useWorld } from '@/context/WorldContext'

export function useLlmActions({ includeDone = false, limit = 50 } = {}) {
  const { currentTick } = useWorld()
  const prevTickRef = useRef(currentTick)
  const [actions, setActions] = useState([])
  const [initialLoading, setInitialLoading] = useState(true)
  const [hasLoaded, setHasLoaded] = useState(false)
  const hasLoadedRef = useRef(false)

  const refresh = useCallback(async () => {
    try {
      const data = await api.getLlmActions({ includeDone, limit })
      setActions(data)
      hasLoadedRef.current = true
      setHasLoaded(true)
      return data
    } catch {
      if (!hasLoadedRef.current) {
        setActions([])
      }
      return null
    } finally {
      setInitialLoading(false)
    }
  }, [includeDone, limit])

  useEffect(() => {
    refresh()
  }, [refresh])

  useEffect(() => {
    if (prevTickRef.current === currentTick) {
      return
    }

    prevTickRef.current = currentTick
    refresh()
  }, [currentTick, refresh])

  return { actions, loading: initialLoading, hasLoaded, refresh }
}
