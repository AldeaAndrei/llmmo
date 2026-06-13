import { Separator } from '@/components/ui/separator'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useWorld } from '@/context/WorldContext'
import { useCityActions } from '@/hooks/useCityActions'

function slotSummary(actions, type, currentTick) {
  const active = actions.find(
    (action) => action.status === 'in_progress' && action.type === type,
  )

  if (!active) {
    return 'Idle'
  }

  if (active.readyAtTick != null) {
    const remaining = Math.max(0, active.readyAtTick - currentTick)
    return `${remaining} tick${remaining === 1 ? '' : 's'} left`
  }

  return 'Active'
}

function TickLine({ currentTick, secondsUntilNextTick }) {
  const countdown =
    secondsUntilNextTick != null ? `${secondsUntilNextTick}s` : '…'

  return (
    <span>
      Tick {currentTick} · next in {countdown}
    </span>
  )
}

function StatusBar() {
  const { isAuthenticated } = useAuth()
  const { primaryCity, loading } = useGameData()
  const { currentTick, secondsUntilNextTick, loading: worldLoading } = useWorld()
  const { actions } = useCityActions(isAuthenticated ? primaryCity?.id : null)

  if (worldLoading && currentTick === 0) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Loading world…
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="flex flex-wrap items-center gap-3 border-b px-4 py-2 text-sm text-muted-foreground">
        <TickLine
          currentTick={currentTick}
          secondsUntilNextTick={secondsUntilNextTick}
        />
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>Log in to play</span>
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
      <div className="flex flex-wrap items-center gap-3 border-b px-4 py-2 text-sm text-muted-foreground">
        <TickLine
          currentTick={currentTick}
          secondsUntilNextTick={secondsUntilNextTick}
        />
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>No city found</span>
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
      <TickLine
        currentTick={currentTick}
        secondsUntilNextTick={secondsUntilNextTick}
      />
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Upgrade: {slotSummary(actions, 'upgrade', currentTick)}</span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Train: {slotSummary(actions, 'train', currentTick)}</span>
    </div>
  )
}

export default StatusBar
