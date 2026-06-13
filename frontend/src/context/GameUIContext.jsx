import { createContext, useCallback, useContext, useMemo, useState } from 'react'

import { HOME_TILE, tileId } from '@/lib/map'

const DEFAULT_SELECTION = {
  map: { type: 'tile', id: tileId(HOME_TILE.x, HOME_TILE.y) },
  city: { type: 'building', id: 'slot-1' },
}

const GameUIContext = createContext(null)

export function GameUIProvider({ children }) {
  const [activeTab, setActiveTabState] = useState('map')
  const [selection, setSelection] = useState(DEFAULT_SELECTION.map)

  const setActiveTab = useCallback((tab) => {
    setActiveTabState(tab)
    setSelection(DEFAULT_SELECTION[tab])
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
