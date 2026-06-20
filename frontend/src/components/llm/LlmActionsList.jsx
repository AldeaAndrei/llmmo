import { useState } from 'react'

import { Button } from '@/components/ui/button'
import LlmActionCard from '@/components/llm/LlmActionCard'
import { useLlmActions } from '@/hooks/useLlmActions'
import { useTickTime } from '@/hooks/useTickTime'
import { useWorld } from '@/context/WorldContext'

function LlmActionsList({ showHeader = true, defaultIncludeDone = false }) {
  const [includeDone, setIncludeDone] = useState(defaultIncludeDone)
  const { currentTick } = useWorld()
  const { formatRemainingLabel, formatTicksAsDuration } = useTickTime()
  const { actions, loading, hasLoaded } = useLlmActions({ includeDone })

  return (
    <div className="flex h-full min-h-0 flex-col p-4">
      {showHeader && (
        <div className="mb-4 shrink-0 space-y-2">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 className="font-medium">LLM agent activity</h2>
              <p className="text-sm text-muted-foreground">
                Live actions from AI agents playing the world
              </p>
            </div>
            <Button
              type="button"
              variant={includeDone ? 'default' : 'outline'}
              size="sm"
              onClick={() => setIncludeDone((value) => !value)}
            >
              {includeDone ? 'Hide completed' : 'Show completed'}
            </Button>
          </div>
        </div>
      )}

      {loading && !hasLoaded && (
        <div className="flex flex-1 items-center justify-center text-sm text-muted-foreground">
          Loading agent actions…
        </div>
      )}

      {hasLoaded && actions.length === 0 && (
        <div className="flex flex-1 items-center justify-center text-sm text-muted-foreground">
          No LLM actions yet. Agents will appear here when they upgrade or train.
        </div>
      )}

      {actions.length > 0 && (
        <ul className="min-h-0 flex-1 space-y-2 overflow-y-auto">
          {actions.map((action) => (
            <LlmActionCard
              key={action.id}
              action={action}
              currentTick={currentTick}
              formatRemainingLabel={formatRemainingLabel}
              formatTicksAsDuration={formatTicksAsDuration}
            />
          ))}
        </ul>
      )}
    </div>
  )
}

export default LlmActionsList
