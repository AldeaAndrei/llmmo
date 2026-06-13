import { AuthProvider } from '@/context/AuthContext'
import { GameDataProvider } from '@/context/GameDataContext'
import { GameUIProvider } from '@/context/GameUIContext'
import AuthGate from '@/components/auth/AuthGate'
import GameBootstrap from '@/components/GameBootstrap'
import AppShell from '@/components/layout/AppShell'

function App() {
  return (
    <AuthProvider>
      <GameUIProvider>
        <GameDataProvider>
          <AuthGate>
            <GameBootstrap />
            <AppShell />
          </AuthGate>
        </GameDataProvider>
      </GameUIProvider>
    </AuthProvider>
  )
}

export default App
