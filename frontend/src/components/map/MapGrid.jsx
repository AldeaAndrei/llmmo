import { cn } from '@/lib/utils'
import { useGameUI } from '@/context/GameUIContext'

const FAKE_TILES = [
  { id: 'home', label: 'Home' },
  { id: 'tile-2', label: '2' },
  { id: 'tile-3', label: '3' },
  { id: 'tile-4', label: '4' },
]

function MapGrid() {
  const { selection, setSelection } = useGameUI()

  return (
    <div className="flex h-full flex-col p-4">
      <p className="mb-4 text-sm text-muted-foreground">
        Map grid (pan/zoom later)
      </p>
      <div className="flex flex-1 items-center justify-center rounded-lg border border-dashed border-border">
        <div className="grid grid-cols-2 gap-2">
          {FAKE_TILES.map((tile) => {
            const isSelected =
              selection?.type === 'tile' && selection.id === tile.id

            return (
              <button
                key={tile.id}
                type="button"
                onClick={() => setSelection({ type: 'tile', id: tile.id })}
                className={cn(
                  'flex h-20 w-20 items-center justify-center rounded-md border text-sm transition-colors',
                  isSelected
                    ? 'border-primary bg-primary/10'
                    : 'border-border bg-muted/50 hover:bg-muted',
                )}
              >
                {tile.label}
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export default MapGrid
