import { Separator } from '@/components/ui/separator'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useWorld } from '@/context/WorldContext'
import { useCityActions } from '@/hooks/useCityActions'
import { useTickTime } from '@/hooks/useTickTime'

function slotSummary(actions, type, currentTick, formatRemainingTicks) {
  const active = actions.find(
    (action) => action.status === 'in_progress' && action.type === type,
  )

  if (!active) {
    return 'Idle'
  }

  if (active.readyAtTick != null) {
    const remaining = Math.max(0, active.readyAtTick - currentTick)
    if (remaining === 0) {
      return 'Ready'
    }

    return `${formatRemainingTicks(remaining)} left`
  }

  return 'Active'
}

function TickLine({ secondsUntilNextTick, formatDuration }) {
  const countdown =
    secondsUntilNextTick != null
      ? formatDuration(secondsUntilNextTick)
      : '…'

  return <span>Next update in {countdown}</span>
}

function formatTickDelta(delta) {
  if (delta > 0) {
    return `+${delta}`
  }

  if (delta < 0) {
    return String(delta)
  }

  return '+0'
}

function formatResource(label, resource) {
  if (!resource) {
    return `${label} —`
  }

  let line = `${label} ${resource.amount}/${resource.max} ${formatTickDelta(resource.tickDelta)}`

  if (resource.upkeep > 0) {
    line += ` (−${resource.upkeep} upkeep)`
  }

  return line
}

function StatusBar() {
  const { isAuthenticated } = useAuth()
  const { primaryCity, loading } = useGameData()
  const { currentTick, secondsUntilNextTick, loading: worldLoading } = useWorld()
  const { formatDuration, formatRemainingTicks } = useTickTime()
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
          secondsUntilNextTick={secondsUntilNextTick}
          formatDuration={formatDuration}
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
          secondsUntilNextTick={secondsUntilNextTick}
          formatDuration={formatDuration}
        />
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>No city found</span>
      </div>
    )
  }

  const troopLine = (primaryCity.troops ?? [])
    .map((t) => `${t.type}: ${t.quantity}`)
    .join(' ')

  const resources = primaryCity.resources

  return (
    <div className="space-y-1 border-b px-4 py-2 text-sm text-muted-foreground">
      <div className="flex flex-wrap items-center gap-3">
        <span>
          {formatResource('Gold', resources?.gold)} ·{' '}
          {formatResource('Stone', resources?.stone)} ·{' '}
          {formatResource('Wood', resources?.wood)} ·{' '}
          {formatResource('Food', resources?.food)}
        </span>
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>{primaryCity.name}</span>
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <TickLine
          secondsUntilNextTick={secondsUntilNextTick}
          formatDuration={formatDuration}
        />
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>
          Upgrade:{' '}
          {slotSummary(actions, 'upgrade', currentTick, formatRemainingTicks)}
        </span>
        <Separator orientation="vertical" className="hidden h-4 sm:block" />
        <span>
          Train: {slotSummary(actions, 'train', currentTick, formatRemainingTicks)}
        </span>
      </div>
      {troopLine && <div>{troopLine}</div>}
    </div>
  )
}

export default StatusBar
