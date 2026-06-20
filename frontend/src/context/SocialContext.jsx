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

const SocialContext = createContext(null)

export function SocialProvider({ children }) {
  const { isAuthenticated } = useAuth()
  const { actionRevision } = useGameData()
  const { currentTick } = useWorld()
  const [players, setPlayers] = useState([])
  const [messages, setMessages] = useState([])
  const [relations, setRelations] = useState([])
  const [cooldowns, setCooldowns] = useState(null)
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
      setPlayers([])
      setMessages([])
      setRelations([])
      setCooldowns(null)
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
        const [playersData, messagesData, relationsData, cooldownsData] =
          await Promise.all([
            api.getDiplomacyPlayers(),
            api.getDiplomacyMessages(),
            api.getDiplomacyRelations(),
            api.getDiplomacyCooldowns(),
          ])

        if (cancelled) return

        setPlayers(playersData)
        setMessages(messagesData)
        setRelations(relationsData)
        setCooldowns(cooldownsData)
        hasLoadedRef.current = true
        setHasLoaded(true)
      } catch {
        if (cancelled || hasLoadedRef.current) return
        setPlayers([])
        setMessages([])
        setRelations([])
        setCooldowns(null)
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

  const sendMessage = useCallback(
    async (toPlayerId, subject, body) => {
      const message = await api.sendDiplomacyMessage({
        toPlayerId,
        subject,
        body,
      })
      setMessages((current) => [message, ...current])
      refresh()
      return message
    },
    [refresh],
  )

  const markMessageRead = useCallback(async (messageId) => {
    setMessages((current) =>
      current.map((message) =>
        message.id === messageId && !message.readAt
          ? { ...message, readAt: new Date().toISOString() }
          : message,
      ),
    )

    try {
      const updated = await api.markDiplomacyMessageRead(messageId)
      setMessages((current) =>
        current.map((message) => (message.id === messageId ? updated : message)),
      )
    } catch {
      refresh()
    }
  }, [refresh])

  const setRelation = useCallback(
    async (toPlayerId, relation) => {
      const updated = await api.setDiplomacyRelation({ toPlayerId, relation })
      setRelations((current) => {
        const without = current.filter(
          (entry) => entry.otherPlayerId !== toPlayerId,
        )
        return [...without, updated].sort((a, b) =>
          a.otherPlayerName.localeCompare(b.otherPlayerName),
        )
      })
      refresh()
      return updated
    },
    [refresh],
  )

  const clearRelation = useCallback(
    async (toPlayerId) => {
      await api.clearDiplomacyRelation(toPlayerId)
      setRelations((current) =>
        current.filter((entry) => entry.otherPlayerId !== toPlayerId),
      )
      refresh()
    },
    [refresh],
  )

  const value = useMemo(
    () => ({
      players,
      messages,
      relations,
      cooldowns,
      loading: initialLoading,
      hasLoaded,
      sendMessage,
      markMessageRead,
      setRelation,
      clearRelation,
      refresh,
    }),
    [
      players,
      messages,
      relations,
      cooldowns,
      initialLoading,
      hasLoaded,
      sendMessage,
      markMessageRead,
      setRelation,
      clearRelation,
      refresh,
    ],
  )

  return (
    <SocialContext.Provider value={value}>{children}</SocialContext.Provider>
  )
}

export function useSocial() {
  const context = useContext(SocialContext)
  if (!context) {
    throw new Error('useSocial must be used within SocialProvider')
  }
  return context
}
