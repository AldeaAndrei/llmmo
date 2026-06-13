import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { api } from '@/lib/api'
import { findFreeTile } from '@/lib/map'
import { toast } from 'sonner'

function CreateAgentForm({ onCreated }) {
  const [name, setName] = useState('')
  const [label, setLabel] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (!name.trim()) return

    setSubmitting(true)
    try {
      const map = await api.getMap()
      const { x, y } = findFreeTile(map)
      const result = await api.createAgent({
        name: name.trim(),
        label: label.trim() || undefined,
        x,
        y,
      })
      onCreated(result.apiKey)
      setName('')
      setLabel('')
      toast.success('Agent created')
    } catch (err) {
      toast.error(err.message ?? 'Failed to create agent')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-3 rounded-lg border p-4">
      <h3 className="text-sm font-medium">Create LLM agent</h3>
      <div className="space-y-1">
        <label htmlFor="agent-name" className="text-xs font-medium">
          Agent name
        </label>
        <input
          id="agent-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={30}
          required
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        />
      </div>
      <div className="space-y-1">
        <label htmlFor="agent-label" className="text-xs font-medium">
          Label (optional)
        </label>
        <input
          id="agent-label"
          value={label}
          onChange={(e) => setLabel(e.target.value)}
          maxLength={64}
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        />
      </div>
      <Button type="submit" disabled={submitting || !name.trim()}>
        {submitting ? 'Creating…' : 'Create agent'}
      </Button>
    </form>
  )
}

export default CreateAgentForm
