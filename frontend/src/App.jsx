import { AuthProvider } from '@/context/AuthContext'
import { GameDataProvider } from '@/context/GameDataContext'
import { GameUIProvider } from '@/context/GameUIContext'
import { ReportsProvider } from '@/context/ReportsContext'
import { WorldProvider } from '@/context/WorldContext'
import AuthGate from '@/components/auth/AuthGate'
import GameBootstrap from '@/components/GameBootstrap'
import AppShell from '@/components/layout/AppShell'

function App() {
  return (
    <AuthProvider>
      <WorldProvider>
        <GameUIProvider>
          <GameDataProvider>
            <ReportsProvider>
              <AuthGate>
                <GameBootstrap />
                <AppShell />
              </AuthGate>
            </ReportsProvider>
          </GameDataProvider>
        </GameUIProvider>
      </WorldProvider>
    </AuthProvider>
  )
}

export default App
