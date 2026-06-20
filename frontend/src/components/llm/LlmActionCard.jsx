function formatPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  return Object.entries(payload)
    .filter(([key]) => key !== 'deducted' && key !== 'reason')
    .map(([key, value]) => `${key}: ${value}`)
    .join(', ')
}

function formatDiplomacyType(type) {
  switch (type) {
    case 'message':
      return 'Message'
    case 'ally':
      return 'Alliance'
    case 'enemy':
      return 'Declare War'
    case 'clear_relation':
      return 'Clear Relation'
    default:
      return type
  }
}

function formatDiplomacyDetail(action) {
  const payload = action.payload
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  if (action.type === 'message') {
    const parts = []
    if (payload.toPlayerName) {
      parts.push(`To: ${payload.toPlayerName}`)
    }
    if (payload.subject) {
      parts.push(`Subject: ${payload.subject}`)
    }
    if (payload.body) {
      parts.push(payload.body)
    }
    return parts.join(' · ')
  }

  if (payload.targetPlayerName) {
    return `Target: ${payload.targetPlayerName}`
  }

  return formatPayload(payload)
}

function statusLabel(status) {
  if (!status) return ''
  return status.replaceAll('_', ' ')
}

function formatTiming(
  action,
  currentTick,
  formatRemainingLabel,
  formatTicksAsDuration,
) {
  if (action.category === 'diplomacy') {
    return action.createdAt ? formatTime(action.createdAt) : 'Queued'
  }

  if (action.status === 'in_progress' && action.readyAtTick != null) {
    const remaining = Math.max(0, action.readyAtTick - currentTick)
    return formatRemainingLabel(remaining)
  }

  if (action.status === 'in_progress' && action.durationTicks != null) {
    return `${formatTicksAsDuration(action.durationTicks)} duration`
  }

  return 'Queued'
}

function formatTime(createdAt) {
  if (!createdAt) {
    return ''
  }

  return new Date(createdAt).toLocaleString()
}

function getReason(payload) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const reason = payload.reason
  return typeof reason === 'string' && reason.trim() ? reason.trim() : null
}

function LlmActionCard({
  action,
  currentTick,
  formatRemainingLabel,
  formatTicksAsDuration,
}) {
  const isDiplomacy = action.category === 'diplomacy'
  const payloadText = isDiplomacy
    ? formatDiplomacyDetail(action)
    : formatPayload(action.payload)
  const reason = isDiplomacy ? null : getReason(action.payload)

  return (
    <li className="rounded-md border bg-muted/30 px-3 py-3 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-medium capitalize">
          {isDiplomacy ? formatDiplomacyType(action.type) : action.type}
        </span>
        {action.status && (
          <span className="text-xs capitalize text-muted-foreground">
            {statusLabel(action.status)}
          </span>
        )}
      </div>

      <p className="mt-1 text-xs text-muted-foreground">
        {action.playerName}
        {action.cityName != null ? (
          <>
            {' '}
            · {action.cityName} ({action.cityX}, {action.cityY})
          </>
        ) : isDiplomacy ? (
          ' · Diplomacy'
        ) : null}
      </p>

      <p className="mt-1 text-xs text-muted-foreground">
        {formatTiming(
          action,
          currentTick,
          formatRemainingLabel,
          formatTicksAsDuration,
        )}
        {action.createdAt ? ` · ${formatTime(action.createdAt)}` : ''}
      </p>

      {payloadText && (
        <p className="mt-1 whitespace-pre-wrap text-xs text-muted-foreground">
          {payloadText}
        </p>
      )}

      {reason && (
        <p className="mt-2 text-sm text-foreground/90">
          <span className="font-medium text-muted-foreground">Why: </span>
          {reason}
        </p>
      )}
    </li>
  )
}

export default LlmActionCard
export { formatPayload, getReason }
