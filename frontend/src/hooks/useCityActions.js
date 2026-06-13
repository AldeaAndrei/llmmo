import { useCallback, useEffect, useState } from 'react'
import { api } from '@/lib/api'
import { useGameData } from '@/context/GameDataContext'

export function useCityActions(cityId) {
  const { actionRevision } = useGameData()
  const [actions, setActions] = useState([])
  const [loading, setLoading] = useState(false)

  const refresh = useCallback(async () => {
    if (!cityId) {
      setActions([])
      return
    }

    setLoading(true)
    try {
      const data = await api.getActions(cityId)
      setActions(data)
    } catch {
      setActions([])
    } finally {
      setLoading(false)
    }
  }, [cityId])

  useEffect(() => {
    refresh()
  }, [refresh, actionRevision])

  return { actions, loading, refresh }
}
