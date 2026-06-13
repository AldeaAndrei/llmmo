import { Button } from '@/components/ui/button'

function AgentList({ agents, onReissue, onRevoke }) {
  if (agents.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No agents yet. Create one for your LLM bot.
      </p>
    )
  }

  return (
    <ul className="space-y-3">
      {agents.map((agent) => (
        <li key={agent.playerId} className="rounded-lg border p-4 text-sm">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="font-medium">{agent.name}</p>
              <p className="text-muted-foreground">
                {agent.cityName} · ({agent.x}, {agent.y})
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                Key: {agent.keyPrefix || '—'} · {agent.keyStatus}
                {agent.lastUsedAt
                  ? ` · last used ${new Date(agent.lastUsedAt).toLocaleString()}`
                  : ''}
              </p>
            </div>
            <span
              className={`rounded px-2 py-0.5 text-xs capitalize ${
                agent.keyStatus === 'active'
                  ? 'bg-green-100 text-green-800'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              {agent.keyStatus}
            </span>
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onReissue(agent.playerId)}
            >
              Reissue key
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onRevoke(agent.playerId)}
              disabled={agent.keyStatus !== 'active'}
            >
              Revoke key
            </Button>
          </div>
        </li>
      ))}
    </ul>
  )
}

export default AgentList
