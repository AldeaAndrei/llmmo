import { AuthProvider } from '@/context/AuthContext'
import { GameDataProvider } from '@/context/GameDataContext'
import { GameUIProvider } from '@/context/GameUIContext'
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
            <AuthGate>
              <GameBootstrap />
              <AppShell />
            </AuthGate>
          </GameDataProvider>
        </GameUIProvider>
      </WorldProvider>
    </AuthProvider>
  )
}

export default App
