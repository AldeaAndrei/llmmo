export function formatDuration(totalSeconds) {
  const seconds = Math.max(0, Math.floor(totalSeconds))
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  return [hours, minutes, secs]
    .map((value) => String(value).padStart(2, '0'))
    .join(':')
}

export function ticksToDurationSeconds(ticks, tickIntervalSeconds) {
  return Math.max(0, ticks ?? 0) * tickIntervalSeconds
}

export function remainingSecondsFromTicks(
  remainingTicks,
  tickIntervalSeconds,
  secondsUntilNextTick = null,
) {
  const ticks = Math.max(0, remainingTicks ?? 0)

  if (ticks === 0) {
    return 0
  }

  if (secondsUntilNextTick != null) {
    return (ticks - 1) * tickIntervalSeconds + secondsUntilNextTick
  }

  return ticks * tickIntervalSeconds
}

export function formatTicksAsDuration(ticks, tickIntervalSeconds) {
  return formatDuration(ticksToDurationSeconds(ticks, tickIntervalSeconds))
}

export function formatRemainingTicks(
  remainingTicks,
  tickIntervalSeconds,
  secondsUntilNextTick = null,
) {
  return formatDuration(
    remainingSecondsFromTicks(
      remainingTicks,
      tickIntervalSeconds,
      secondsUntilNextTick,
    ),
  )
}

export function formatRemainingLabel(
  remainingTicks,
  tickIntervalSeconds,
  secondsUntilNextTick = null,
) {
  const ticks = Math.max(0, remainingTicks ?? 0)

  if (ticks === 0) {
    return 'Ready'
  }

  return `${formatRemainingTicks(ticks, tickIntervalSeconds, secondsUntilNextTick)} remaining`
}

export function formatPlayerEffectText(text, tickIntervalSeconds) {
  if (!text) {
    return text
  }

  const interval = formatTicksAsDuration(1, tickIntervalSeconds)

  return text
    .replace(/\bper level per tick\b/gi, `per level every ${interval}`)
    .replace(/\bper tick\b/gi, `every ${interval}`)
}
