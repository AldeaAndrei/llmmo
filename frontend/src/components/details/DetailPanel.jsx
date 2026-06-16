import { ScrollArea } from '@/components/ui/scroll-area'
import { useGameUI } from '@/context/GameUIContext'
import EmptyDetail from '@/components/details/EmptyDetail'
import MapTileDetail from '@/components/details/MapTileDetail'
import BuildingDetail from '@/components/details/BuildingDetail'
import ReportDetail from '@/components/details/ReportDetail'

function DetailContent() {
  const { selection, activeTab } = useGameUI()

  if (!selection) {
    if (activeTab === 'reports') {
      return (
        <div className="text-sm text-muted-foreground">
          Select a report to view details.
        </div>
      )
    }

    return <EmptyDetail />
  }

  if (selection.type === 'tile') {
    return <MapTileDetail selection={selection} />
  }

  if (selection.type === 'building') {
    return <BuildingDetail selection={selection} />
  }

  if (selection.type === 'report') {
    return <ReportDetail selection={selection} />
  }

  return <EmptyDetail />
}

function DetailPanel() {
  return (
    <ScrollArea className="h-full">
      <div className="p-4">
        <DetailContent />
      </div>
    </ScrollArea>
  )
}

export default DetailPanel
