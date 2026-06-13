import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/context/AuthContext'
import { useGameUI } from '@/context/GameUIContext'

function Navbar() {
  const { activeTab, setActiveTab } = useGameUI()
  const { user, isHuman, logout } = useAuth()

  return (
    <header className="flex items-center justify-between gap-4 border-b px-4 py-2">
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="map">Map</TabsTrigger>
          <TabsTrigger value="city">City</TabsTrigger>
          {isHuman && <TabsTrigger value="agents">Agents</TabsTrigger>}
        </TabsList>
      </Tabs>

      {user && (
        <div className="flex items-center gap-3 text-sm text-muted-foreground">
          <span className="hidden sm:inline">{user.email}</span>
          <Button type="button" variant="outline" size="sm" onClick={logout}>
            Log out
          </Button>
        </div>
      )}
    </header>
  )
}

export default Navbar
