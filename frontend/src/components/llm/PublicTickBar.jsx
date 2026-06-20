import { useWorld } from '@/context/WorldContext'
import { useTickTime } from '@/hooks/useTickTime'

function PublicTickBar() {
  const { secondsUntilNextTick, loading, currentTick } = useWorld()
  const { formatDuration } = useTickTime()

  if (loading && currentTick === 0) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Loading world…
      </div>
    )
  }

  const countdown =
    secondsUntilNextTick != null
      ? formatDuration(secondsUntilNextTick)
      : '…'

  return (
    <div className="border-b px-4 py-2 text-sm text-muted-foreground">
      Next update in {countdown}
    </div>
  )
}

export default PublicTickBar
