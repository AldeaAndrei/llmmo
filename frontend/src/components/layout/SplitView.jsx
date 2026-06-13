import { useGameUI } from '@/context/GameUIContext'
import { useIsMobile } from '@/hooks/useIsMobile'
import MapGrid from '@/components/map/MapGrid'
import CityGrid from '@/components/city/CityGrid'
import AgentsPanel from '@/components/agents/AgentsPanel'
import DetailPanel from '@/components/details/DetailPanel'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'

function SplitView() {
  const { activeTab, selection, clearSelection } = useGameUI()
  const isMobile = useIsMobile()

  const LeftPanel =
    activeTab === 'map' ? MapGrid : activeTab === 'city' ? CityGrid : AgentsPanel

  const showDetailPanel = activeTab !== 'agents'

  return (
    <>
      <div className="flex min-h-0 flex-1 overflow-hidden">
        <div className="h-full min-h-0 min-w-0 flex-1 overflow-hidden md:border-r">
          <LeftPanel />
        </div>
        {showDetailPanel && (
          <aside className="hidden h-full w-[380px] shrink-0 overflow-hidden md:block">
            <DetailPanel />
          </aside>
        )}
      </div>

      {showDetailPanel && (
        <Sheet
          open={isMobile && selection !== null}
          onOpenChange={(open) => {
            if (!open) {
              clearSelection()
            }
          }}
        >
          <SheetContent side="right" className="w-full sm:max-w-md md:hidden">
            <SheetHeader>
              <SheetTitle>Details</SheetTitle>
            </SheetHeader>
            <DetailPanel />
          </SheetContent>
        </Sheet>
      )}
    </>
  )
}

export default SplitView
