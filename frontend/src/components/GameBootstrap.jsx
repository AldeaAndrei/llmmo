import { useEffect } from 'react'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'
import { tileId } from '@/lib/map'

function GameBootstrap() {
  const { primaryCity, isAuthenticated } = useGameData()
  const { setSelection, activeTab } = useGameUI()

  useEffect(() => {
    if (!isAuthenticated || !primaryCity) return

    if (activeTab === 'map') {
      setSelection({ type: 'tile', id: tileId(primaryCity.x, primaryCity.y) })
    }
  }, [
    primaryCity?.id,
    primaryCity?.x,
    primaryCity?.y,
    isAuthenticated,
    setSelection,
    activeTab,
  ])

  return null
}

export default GameBootstrap
