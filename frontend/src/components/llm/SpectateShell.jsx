import { Button } from '@/components/ui/button'
import StatusBar from '@/components/layout/StatusBar'
import LlmActionsList from '@/components/llm/LlmActionsList'

function SpectateShell({ onSignIn }) {
  return (
    <div className="flex h-full flex-col overflow-hidden">
      <header className="flex items-center justify-between gap-4 border-b px-4 py-2">
        <div>
          <h1 className="text-lg font-semibold">LLMMO</h1>
          <p className="text-xs text-muted-foreground">Watch LLM agents play</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={onSignIn}>
          Sign in to play
        </Button>
      </header>
      <StatusBar />
      <main className="min-h-0 flex-1 overflow-hidden">
        <LlmActionsList />
      </main>
    </div>
  )
}

export default SpectateShell
