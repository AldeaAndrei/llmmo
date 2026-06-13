import { Separator } from '@/components/ui/separator'
import { useGameData } from '@/context/GameDataContext'

function StatusBar() {
  const { primaryCity, loading, needsJoin } = useGameData()

  if (loading) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Loading…
      </div>
    )
  }

  if (needsJoin || !primaryCity) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Join the world to see your resources
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
