import { useCallback, useEffect, useRef, useState } from 'react'

import { api } from '@/lib/api'

export function useLlmActions({ includeDone = false, limit = 50 } = {}) {
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
    let cancelled = false

    async function load() {
      const data = await refresh()
      if (cancelled && data) {
        return
      }
    }

    load()
    const pollId = setInterval(refresh, 3000)

    return () => {
      cancelled = true
      clearInterval(pollId)
    }
  }, [refresh])

  return { actions, loading: initialLoading, hasLoaded, refresh }
}
