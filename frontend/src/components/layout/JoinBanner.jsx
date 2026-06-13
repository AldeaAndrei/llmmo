import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useGameData } from '@/context/GameDataContext'

function JoinBanner() {
  const { joinGame, loading, needsJoin } = useGameData()
  const [name, setName] = useState('')

  if (!needsJoin) {
    return null
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return
    await joinGame(trimmed)
  }

  return (
    <div className="border-b bg-muted/40 px-4 py-3">
      <form
        onSubmit={handleSubmit}
        className="mx-auto flex max-w-md flex-wrap items-end gap-2"
      >
        <div className="min-w-[180px] flex-1 space-y-1">
          <label htmlFor="player-name" className="text-xs font-medium">
            Join the world
          </label>
          <input
            id="player-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Your name"
            maxLength={30}
            disabled={loading}
            className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:opacity-50"
          />
        </div>
        <Button type="submit" disabled={loading || !name.trim()}>
          {loading ? 'Joining…' : 'Join'}
        </Button>
      </form>
      <p className="mx-auto mt-2 max-w-md text-xs text-muted-foreground">
        Or open with{' '}
        <code className="rounded bg-muted px-1">?playerId=&lt;uuid&gt;</code> to
        play as a seeded player.
      </p>
    </div>
  )
}

export default JoinBanner
