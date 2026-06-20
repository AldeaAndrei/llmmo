import { useCityActions } from '@/hooks/useCityActions'
import { useTickTime } from '@/hooks/useTickTime'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useWorld } from '@/context/WorldContext'

function formatPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  return Object.entries(payload)
    .filter(([key]) => key !== 'deducted')
    .map(([key, value]) => `${key}: ${value}`)
    .join(', ')
}

function statusLabel(status) {
  return status.replace('_', ' ')
}

function formatTiming(action, currentTick, formatRemainingLabel, formatTicksAsDuration) {
  if (action.status === 'in_progress' && action.readyAtTick != null) {
    const remaining = Math.max(0, action.readyAtTick - currentTick)
    return formatRemainingLabel(remaining)
  }

  if (action.status === 'in_progress' && action.durationTicks != null) {
    return `${formatTicksAsDuration(action.durationTicks)} duration`
  }

  return 'Queued'
}

function CityActionsList({ cityId, title = 'Actions', ownedOnly = false }) {
  const { isAuthenticated, playerId } = useAuth()
  const { cities } = useGameData()
  const { currentTick } = useWorld()
  const { formatRemainingLabel, formatTicksAsDuration } = useTickTime()
  const isOwned =
    !ownedOnly ||
    (cityId && cities.some((city) => city.id === cityId && city.playerId === playerId))

  const { actions, loading, hasLoaded } = useCityActions(isOwned ? cityId : null)
  const visibleActions = actions.filter((action) => action.status !== 'done')

  if (!isAuthenticated) {
    return (
      <div className="space-y-2 border-t pt-4">
        <h3 className="text-sm font-medium">{title}</h3>
        <p className="text-sm text-muted-foreground">Log in to view actions.</p>
      </div>
    )
  }

  if (!cityId || !isOwned) {
    return null
  }

  return (
    <div className="space-y-2 border-t pt-4">
      <h3 className="text-sm font-medium">{title}</h3>

      {loading && !hasLoaded && (
        <p className="text-sm text-muted-foreground">Loading actions…</p>
      )}

      {hasLoaded && visibleActions.length === 0 && (
        <p className="text-sm text-muted-foreground">No actions queued.</p>
      )}

      {visibleActions.length > 0 && (
        <ul className="space-y-2">
          {visibleActions.map((action) => {
            const payloadText = formatPayload(action.payload)

            return (
              <li
                key={action.id}
                className="rounded-md border bg-muted/30 px-3 py-2 text-sm"
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium capitalize">{action.type}</span>
                  <span className="text-xs capitalize text-muted-foreground">
                    {statusLabel(action.status)}
                  </span>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {formatTiming(
                    action,
                    currentTick,
                    formatRemainingLabel,
                    formatTicksAsDuration,
                  )}
                </p>
                {payloadText && (
                  <p className="mt-1 text-xs text-muted-foreground">{payloadText}</p>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

export default CityActionsList
