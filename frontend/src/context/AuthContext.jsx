import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { toast } from 'sonner'

import { api, setUnauthorizedHandler } from '@/lib/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  const clearAuth = useCallback(() => {
    setUser(null)
  }, [])

  const refreshMe = useCallback(async () => {
    try {
      const me = await api.getMe()
      setUser(me)
      return me
    } catch {
      setUser(null)
      return null
    }
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(() => {
      clearAuth()
    })

    refreshMe().finally(() => setLoading(false))
  }, [clearAuth, refreshMe])

  const login = useCallback(async (email, password) => {
    await api.login({ email, password })
    const me = await api.getMe()
    if (!me) {
      throw new Error('Login succeeded but session was not established.')
    }
    setUser(me)
    toast.success(`Welcome back, ${me.playerName}`)
    return me
  }, [])

  const register = useCallback(async ({ email, password, playerName, x, y }) => {
    const result = await api.register({ email, password, playerName, x, y })
    const me = await api.getMe()
    if (!me) {
      throw new Error('Registration succeeded but session was not established.')
    }
    setUser(me)
    toast.success(`Welcome, ${result.playerName}`)
    return result
  }, [])

  const logout = useCallback(async () => {
    try {
      await api.logout()
    } catch {
      // ignore
    }
    clearAuth()
    toast.success('Logged out')
  }, [clearAuth])

  const value = useMemo(
    () => ({
      user,
      playerId: user?.playerId ?? null,
      playerType: user?.playerType ?? null,
      isAuthenticated: !!user,
      isHuman: user?.playerType === 'human',
      loading,
      login,
      register,
      logout,
      refreshMe,
    }),
    [user, loading, login, register, logout, refreshMe],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
