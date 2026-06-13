import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useGameUI } from '@/context/GameUIContext'

function Navbar() {
  const { activeTab, setActiveTab } = useGameUI()

  return (
    <header className="border-b px-4 py-2">
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="map">Map</TabsTrigger>
          <TabsTrigger value="city">City</TabsTrigger>
        </TabsList>
      </Tabs>
    </header>
  )
}

export default Navbar
