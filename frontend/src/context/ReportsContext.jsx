import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { api } from '@/lib/api'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useWorld } from '@/context/WorldContext'

const ReportsContext = createContext(null)

export function ReportsProvider({ children }) {
  const { isAuthenticated } = useAuth()
  const { actionRevision } = useGameData()
  const { currentTick } = useWorld()
  const [reports, setReports] = useState([])
  const [initialLoading, setInitialLoading] = useState(false)
  const [hasLoaded, setHasLoaded] = useState(false)
  const hasLoadedRef = useRef(false)
  const [manualRevision, setManualRevision] = useState(0)

  const refresh = useCallback(() => {
    setManualRevision((revision) => revision + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    if (!isAuthenticated) {
      setReports([])
      setInitialLoading(false)
      setHasLoaded(false)
      hasLoadedRef.current = false
      return
    }

    const isInitialLoad = !hasLoadedRef.current

    if (isInitialLoad) {
      setInitialLoading(true)
    }

    async function load() {
      try {
        const data = await api.getReports()
        if (cancelled) return
        setReports(data)
        hasLoadedRef.current = true
        setHasLoaded(true)
      } catch {
        if (cancelled || hasLoadedRef.current) return
        setReports([])
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
  }, [isAuthenticated, actionRevision, manualRevision, currentTick])

  const markReportRead = useCallback(async (reportId) => {
    setReports((current) =>
      current.map((report) =>
        report.id === reportId && !report.readAt
          ? { ...report, readAt: new Date().toISOString() }
          : report,
      ),
    )

    try {
      const updated = await api.markReportRead(reportId)
      setReports((current) =>
        current.map((report) => (report.id === reportId ? updated : report)),
      )
    } catch {
      refresh()
    }
  }, [refresh])

  const unreadCount = useMemo(
    () => reports.filter((report) => !report.readAt).length,
    [reports],
  )

  const value = useMemo(
    () => ({
      reports,
      loading: initialLoading,
      hasLoaded,
      unreadCount,
      markReportRead,
      refresh,
    }),
    [reports, initialLoading, hasLoaded, unreadCount, markReportRead, refresh],
  )

  return (
    <ReportsContext.Provider value={value}>{children}</ReportsContext.Provider>
  )
}

export function useReports() {
  const context = useContext(ReportsContext)
  if (!context) {
    throw new Error('useReports must be used within ReportsProvider')
  }
  return context
}
