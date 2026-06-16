import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { useLlmActions } from '@/hooks/useLlmActions'
import { useWorld } from '@/context/WorldContext'

function formatPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return ''
  }

  return Object.entries(payload)
    .filter(([key]) => key !== 'deducted')
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

function LlmActionsList({ showHeader = true }) {
  const [includeDone, setIncludeDone] = useState(false)
  const { currentTick } = useWorld()
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
          {actions.map((action) => {
            const payloadText = formatPayload(action.payload)

            return (
              <li
                key={action.id}
                className="rounded-md border bg-muted/30 px-3 py-3 text-sm"
              >
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
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

export default LlmActionsList
