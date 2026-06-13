import { GameUIProvider } from '@/context/GameUIContext'
import AppShell from '@/components/layout/AppShell'

function App() {
  return (
    <GameUIProvider>
      <AppShell />
    </GameUIProvider>
  )
}

export default App
