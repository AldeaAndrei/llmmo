import { createContext, useCallback, useContext, useMemo, useState } from 'react'

import { tileId } from '@/lib/map'

const DEFAULT_SELECTION = {
  map: { type: 'tile', id: tileId(50, 50) },
  city: { type: 'building', id: 'gold_mine' },
  reports: null,
  social: null,
  'llm-activity': null,
}

const GameUIContext = createContext(null)

export function GameUIProvider({ children }) {
  const [activeTab, setActiveTabState] = useState('map')
  const [selection, setSelection] = useState(DEFAULT_SELECTION.map)

  const setActiveTab = useCallback((tab) => {
    setActiveTabState(tab)
    setSelection(DEFAULT_SELECTION[tab] ?? null)
  }, [])

  const clearSelection = useCallback(() => {
    setSelection(null)
  }, [])

  const value = useMemo(
    () => ({
      activeTab,
      selection,
      setActiveTab,
      setSelection,
      clearSelection,
    }),
    [activeTab, selection, setActiveTab, clearSelection],
  )

  return (
    <GameUIContext.Provider value={value}>{children}</GameUIContext.Provider>
  )
}

export function useGameUI() {
  const context = useContext(GameUIContext)
  if (!context) {
    throw new Error('useGameUI must be used within GameUIProvider')
  }
  return context
}
