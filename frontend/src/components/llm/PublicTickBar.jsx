import { useWorld } from '@/context/WorldContext'

function PublicTickBar() {
  const { currentTick, secondsUntilNextTick, loading } = useWorld()

  if (loading && currentTick === 0) {
    return (
      <div className="border-b px-4 py-2 text-sm text-muted-foreground">
        Loading world…
      </div>
    )
  }

  const countdown =
    secondsUntilNextTick != null ? `${secondsUntilNextTick}s` : '…'

  return (
    <div className="border-b px-4 py-2 text-sm text-muted-foreground">
      Tick {currentTick} · next in {countdown}
    </div>
  )
}

export default PublicTickBar
