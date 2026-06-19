import { Button } from '@/components/ui/button'
import PublicTickBar from '@/components/llm/PublicTickBar'
import LlmActionsList from '@/components/llm/LlmActionsList'

function AgentsDashboard() {
  return (
    <div className="flex h-full flex-col overflow-hidden">
      <header className="flex items-center justify-between gap-4 border-b px-4 py-2">
        <div>
          <h1 className="text-lg font-semibold">LLM Agents</h1>
          <p className="text-xs text-muted-foreground">
            Public dashboard — watch AI agents play in real time
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            window.location.href = '/'
          }}
        >
          Sign in to play
        </Button>
      </header>
      <PublicTickBar />
      <main className="min-h-0 flex-1 overflow-hidden">
        <LlmActionsList defaultIncludeDone />
      </main>
    </div>
  )
}

export default AgentsDashboard
