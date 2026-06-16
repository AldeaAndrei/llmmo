import { useTroopMovements } from '@/hooks/useTroopMovements'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'

function formatLocation(location) {
  if (!location) {
    return '(?, ?)'
  }

  return `(${location.x}, ${location.y})`
}

function formatTroops(troops) {
  if (!troops?.length) {
    return null
  }

  return troops.map((troop) => `${troop.type}: ${troop.quantity}`).join(', ')
}

function formatTypeLabel(type) {
  return type === 'scout' ? 'Scout' : 'Attack'
}

function formatPhaseLabel(phase) {
  if (phase === 'returning') {
    return 'returning'
  }

  return 'outbound'
}

function formatTiming(remainingTicks) {
  const ticks = Math.max(0, remainingTicks ?? 0)
  return `${ticks} tick${ticks === 1 ? '' : 's'} remaining`
}

function MovementItem({ movement, variant }) {
  const typeLabel = formatTypeLabel(movement.type)
  const troopsText = formatTroops(movement.troops)
  const endpoint =
    variant === 'outgoing'
      ? formatLocation(movement.target)
      : formatLocation(movement.source)
  const directionLabel =
    variant === 'outgoing' ? `→ ${endpoint}` : `from ${endpoint}`

  return (
    <li className="rounded-md border bg-muted/30 px-3 py-2 text-sm">
      <div className="flex items-center justify-between gap-2">
        <span className="font-medium">
          {typeLabel} {directionLabel}
        </span>
        <span className="text-xs capitalize text-muted-foreground">
          {formatPhaseLabel(movement.phase)}
        </span>
      </div>
      <p className="mt-1 text-xs text-muted-foreground">
        {formatTiming(movement.remainingTicks)}
      </p>
      {troopsText && (
        <p className="mt-1 text-xs text-muted-foreground">{troopsText}</p>
      )}
    </li>
  )
}

function MovementSection({ title, variant, items, emptyText, showEmpty }) {
  if (!showEmpty && items.length === 0) {
    return null
  }

  return (
    <div className="space-y-2">
      <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {title}
      </h4>
      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">{emptyText}</p>
      ) : (
        <ul className="space-y-2">
          {items.map((movement) => (
            <MovementItem
              key={movement.id}
              movement={movement}
              variant={variant}
            />
          ))}
        </ul>
      )}
    </div>
  )
}

function TroopMovementsList({ cityId, title = 'Troop movements', ownedOnly = false }) {
  const { isAuthenticated, playerId } = useAuth()
  const { cities } = useGameData()
  const isOwned =
    !ownedOnly ||
    (cityId && cities.some((city) => city.id === cityId && city.playerId === playerId))

  const { movements, loading, hasLoaded } = useTroopMovements(isOwned ? cityId : null)

  if (!isAuthenticated) {
    return (
      <div className="space-y-2 border-t pt-4">
        <h3 className="text-sm font-medium">{title}</h3>
        <p className="text-sm text-muted-foreground">Log in to view troop movements.</p>
      </div>
    )
  }

  if (!cityId || !isOwned) {
    return null
  }

  return (
    <div className="space-y-4 border-t pt-4">
      <h3 className="text-sm font-medium">{title}</h3>

      {loading && !hasLoaded && (
        <p className="text-sm text-muted-foreground">Loading troop movements…</p>
      )}

      {hasLoaded && (
        <>
          <MovementSection
            title="By you"
            variant="outgoing"
            items={movements.outgoing}
            emptyText="No outgoing attacks or scouts."
            showEmpty
          />
          <MovementSection
            title="To you"
            variant="incoming"
            items={movements.incoming}
            emptyText="No incoming attacks or scouts."
            showEmpty
          />
        </>
      )}
    </div>
  )
}

export default TroopMovementsList
