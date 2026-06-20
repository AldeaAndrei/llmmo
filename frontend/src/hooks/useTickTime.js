import { useWorld } from '@/context/WorldContext'
import {
  formatDuration,
  formatRemainingLabel,
  formatRemainingTicks,
  formatTicksAsDuration,
  formatPlayerEffectText,
  remainingSecondsFromTicks,
} from '@/lib/time'

export function useTickTime() {
  const { tickIntervalSeconds, secondsUntilNextTick } = useWorld()

  return {
    tickIntervalSeconds,
    secondsUntilNextTick,
    formatDuration,
    formatRemainingLabel: (remainingTicks) =>
      formatRemainingLabel(
        remainingTicks,
        tickIntervalSeconds,
        secondsUntilNextTick,
      ),
    formatRemainingTicks: (remainingTicks) =>
      formatRemainingTicks(
        remainingTicks,
        tickIntervalSeconds,
        secondsUntilNextTick,
      ),
    formatTicksAsDuration: (ticks) =>
      formatTicksAsDuration(ticks, tickIntervalSeconds),
    formatPlayerEffectText: (text) =>
      formatPlayerEffectText(text, tickIntervalSeconds),
    remainingSecondsFromTicks: (remainingTicks) =>
      remainingSecondsFromTicks(
        remainingTicks,
        tickIntervalSeconds,
        secondsUntilNextTick,
      ),
  }
}
