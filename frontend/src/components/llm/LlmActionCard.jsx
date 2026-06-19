function formatPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  return Object.entries(payload)
    .filter(([key]) => key !== 'deducted' && key !== 'reason')
    .map(([key, value]) => `${key}: ${value}`)
    .join(', ')
}

function statusLabel(status) {
  return status.replaceAll('_', ' ')
}

function formatTiming(action, currentTick) {
  if (action.status === 'in_progress' && action.readyAtTick != null) {
    const remaining = Math.max(0, action.readyAtTick - currentTick)
    return `${remaining} tick${remaining === 1 ? '' : 's'} remaining`
  }

  if (action.status === 'in_progress') {
    return `${action.durationTicks} tick${action.durationTicks === 1 ? '' : 's'} duration`
  }

  return `Submitted tick ${action.submittedAtTick}`
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

function LlmActionCard({ action, currentTick }) {
  const payloadText = formatPayload(action.payload)
  const reason = getReason(action.payload)

  return (
    <li className="rounded-md border bg-muted/30 px-3 py-3 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-medium capitalize">{action.type}</span>
        <span className="text-xs capitalize text-muted-foreground">
          {statusLabel(action.status)}
        </span>
      </div>

      <p className="mt-1 text-xs text-muted-foreground">
        {action.playerName} · {action.cityName} ({action.cityX},{' '}
        {action.cityY})
      </p>

      <p className="mt-1 text-xs text-muted-foreground">
        {formatTiming(action, currentTick)}
        {action.createdAt ? ` · ${formatTime(action.createdAt)}` : ''}
      </p>

      {payloadText && (
        <p className="mt-1 text-xs text-muted-foreground">{payloadText}</p>
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
