import { ScrollArea } from '@/components/ui/scroll-area'
import { useGameUI } from '@/context/GameUIContext'
import EmptyDetail from '@/components/details/EmptyDetail'
import MapTileDetail from '@/components/details/MapTileDetail'
import BuildingDetail from '@/components/details/BuildingDetail'

function DetailContent() {
  const { selection } = useGameUI()

  if (!selection) {
    return <EmptyDetail />
  }

  if (selection.type === 'tile') {
    return <MapTileDetail selection={selection} />
  }

  if (selection.type === 'building') {
    return <BuildingDetail selection={selection} />
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
