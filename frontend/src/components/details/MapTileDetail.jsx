import { Button } from '@/components/ui/button'
import { isHomeTile, parseTileId } from '@/lib/map'

function MapTileDetail({ selection }) {
  const { x, y } = parseTileId(selection.id)

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Tile</h2>
        <p className="text-sm text-muted-foreground">
          ({x}, {y})
          {isHomeTile(x, y) ? ' · Your city' : ''}
        </p>
      </div>
      <p className="text-sm">Placeholder tile description.</p>
      <Button variant="outline">Action placeholder</Button>
    </div>
  )
}

export default MapTileDetail
