import { AuthProvider } from '@/context/AuthContext'
import { GameDataProvider } from '@/context/GameDataContext'
import { GameUIProvider } from '@/context/GameUIContext'
import { ReportsProvider } from '@/context/ReportsContext'
import { SocialProvider } from '@/context/SocialContext'
import { WorldProvider } from '@/context/WorldContext'
import AuthGate from '@/components/auth/AuthGate'
import GameBootstrap from '@/components/GameBootstrap'
import AppShell from '@/components/layout/AppShell'
import AgentsDashboard from '@/components/llm/AgentsDashboard'

function isAgentsDashboard() {
  const path = window.location.pathname
  return path === '/agents' || path.endsWith('/agents')
}

function App() {
  if (isAgentsDashboard()) {
    return (
      <WorldProvider>
        <AgentsDashboard />
      </WorldProvider>
    )
  }

  return (
    <AuthProvider>
      <WorldProvider>
        <GameUIProvider>
          <GameDataProvider>
            <ReportsProvider>
              <SocialProvider>
                <AuthGate>
                  <GameBootstrap />
                  <AppShell />
                </AuthGate>
              </SocialProvider>
            </ReportsProvider>
          </GameDataProvider>
        </GameUIProvider>
      </WorldProvider>
    </AuthProvider>
  )
}

export default App
