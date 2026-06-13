import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'

import { api } from '@/lib/api'
import AgentList from '@/components/agents/AgentList'
import CreateAgentForm from '@/components/agents/CreateAgentForm'
import KeyRevealDialog from '@/components/agents/KeyRevealDialog'

function AgentsPanel() {
  const [agents, setAgents] = useState([])
  const [loading, setLoading] = useState(true)
  const [revealedKey, setRevealedKey] = useState(null)

  const loadAgents = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.listAgents()
      setAgents(data)
    } catch (err) {
      toast.error(err.message ?? 'Failed to load agents')
      setAgents([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadAgents()
  }, [loadAgents])

  const handleCreated = (apiKey) => {
    setRevealedKey(apiKey)
    loadAgents()
  }

  const handleReissue = async (playerId) => {
    try {
      const result = await api.reissueAgentKey(playerId)
      setRevealedKey(result.apiKey)
      loadAgents()
      toast.success('Key reissued')
    } catch (err) {
      toast.error(err.message ?? 'Failed to reissue key')
    }
  }

  const handleRevoke = async (playerId) => {
    try {
      await api.revokeAgentKey(playerId)
      loadAgents()
      toast.success('Key revoked')
    } catch (err) {
      toast.error(err.message ?? 'Failed to revoke key')
    }
  }

  return (
    <div className="h-full overflow-auto p-4">
      <div className="mx-auto max-w-2xl space-y-6">
        <div>
          <h2 className="text-lg font-semibold">LLM Agents</h2>
          <p className="text-sm text-muted-foreground">
            Create and manage API keys for LLM players. Each agent gets its own
            city and Bearer token.
          </p>
        </div>

        <CreateAgentForm onCreated={handleCreated} />

        {loading ? (
          <p className="text-sm text-muted-foreground">Loading agents…</p>
        ) : (
          <AgentList
            agents={agents}
            onReissue={handleReissue}
            onRevoke={handleRevoke}
          />
        )}
      </div>

      <KeyRevealDialog
        apiKey={revealedKey}
        title="API key created"
        onClose={() => setRevealedKey(null)}
      />
    </div>
  )
}

export default AgentsPanel
