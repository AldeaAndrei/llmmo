import { Separator } from '@/components/ui/separator'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'

function StatusBar() {
  const { isAuthenticated } = useAuth()
  const { primaryCity, loading } = useGameData()

  if (!isAuthenticated) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Log in to play
      </div>
    )
  }

  if (loading) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Loading…
      </div>
    )
  }

  if (!primaryCity) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        No city found
      </div>
    )
  }

  return (
    <div className="flex flex-wrap items-center gap-3 border-b px-4 py-2 text-sm text-muted-foreground">
      <span>
        Gold {primaryCity.gold} · Stone {primaryCity.stone} · Wood{' '}
        {primaryCity.wood} · Food {primaryCity.food} · Troops{' '}
        {primaryCity.troopCount}
      </span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>{primaryCity.name}</span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Queue: —</span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Actions: —</span>
    </div>
  )
}

export default StatusBar
