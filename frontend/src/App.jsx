import { GameDataProvider } from '@/context/GameDataContext'
import { GameUIProvider } from '@/context/GameUIContext'
import GameBootstrap from '@/components/GameBootstrap'
import AppShell from '@/components/layout/AppShell'

function App() {
  return (
    <GameUIProvider>
      <GameDataProvider>
        <GameBootstrap />
        <AppShell />
      </GameDataProvider>
    </GameUIProvider>
  )
}

export default App
