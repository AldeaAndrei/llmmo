import { Button } from '@/components/ui/button'

function MapTileDetail({ selection }) {
  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Tile</h2>
        <p className="text-sm text-muted-foreground">{selection.id}</p>
      </div>
      <p className="text-sm">Placeholder tile description.</p>
      <Button variant="outline">Action placeholder</Button>
    </div>
  )
}

export default MapTileDetail
