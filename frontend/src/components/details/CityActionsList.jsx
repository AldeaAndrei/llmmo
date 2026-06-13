import { useCityActions } from '@/hooks/useCityActions'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'

function formatPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  return Object.entries(payload)
    .map(([key, value]) => `${key}: ${value}`)
    .join(', ')
}

function statusLabel(status) {
  return status.replace('_', ' ')
}

function CityActionsList({ cityId, title = 'Actions', ownedOnly = false }) {
  const { isAuthenticated, playerId } = useAuth()
  const { cities } = useGameData()
  const isOwned =
    !ownedOnly ||
    (cityId && cities.some((city) => city.id === cityId && city.playerId === playerId))

  const { actions, loading } = useCityActions(isOwned ? cityId : null)

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

      {loading && (
        <p className="text-sm text-muted-foreground">Loading actions…</p>
      )}

      {!loading && actions.length === 0 && (
        <p className="text-sm text-muted-foreground">No actions queued.</p>
      )}

      {!loading && actions.length > 0 && (
        <ul className="space-y-2">
          {actions.map((action) => {
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
                  Tick {action.submittedAtTick}
                  {action.readyAtTick != null
                    ? ` → ${action.readyAtTick}`
                    : ` · ${action.durationTicks} tick${action.durationTicks === 1 ? '' : 's'}`}
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
