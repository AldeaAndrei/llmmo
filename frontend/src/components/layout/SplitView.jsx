import { useGameUI } from '@/context/GameUIContext'
import { useIsMobile } from '@/hooks/useIsMobile'
import MapGrid from '@/components/map/MapGrid'
import CityGrid from '@/components/city/CityGrid'
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
  const LeftPanel = activeTab === 'map' ? MapGrid : CityGrid

  return (
    <>
      <div className="flex min-h-0 flex-1">
        <div className="min-h-0 flex-1 md:border-r">
          <LeftPanel />
        </div>
        <aside className="hidden w-[380px] shrink-0 md:block">
          <DetailPanel />
        </aside>
      </div>

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
    </>
  )
}

export default SplitView
