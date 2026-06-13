import { cn } from '@/lib/utils'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'

const CITY_SLOTS = Array.from({ length: 8 }, (_, index) => ({
  id: `slot-${index + 1}`,
  label: `${index + 1}`,
}))

function CityGrid() {
  const { selection, setSelection } = useGameUI()
  const { primaryCity, loading, needsJoin } = useGameData()

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Loading city…
      </div>
    )
  }

  if (needsJoin || !primaryCity) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Join the world to view your city
      </div>
    )
  }

  return (
    <div className="flex h-full flex-col p-4">
      <div className="mb-4">
        <h2 className="font-medium">{primaryCity.name}</h2>
        <p className="text-sm text-muted-foreground">
          ({primaryCity.x}, {primaryCity.y}) · Troops {primaryCity.troopCount}
        </p>
      </div>
      <div className="flex flex-1 items-center justify-center rounded-lg border border-dashed border-border">
        <div className="grid grid-cols-4 gap-2">
          {CITY_SLOTS.map((slot) => {
            const isSelected =
              selection?.type === 'building' && selection.id === slot.id

            return (
              <button
                key={slot.id}
                type="button"
                onClick={() =>
                  setSelection({ type: 'building', id: slot.id })
                }
                className={cn(
                  'flex h-16 w-16 items-center justify-center rounded-md border text-sm transition-colors',
                  isSelected
                    ? 'border-primary bg-primary/10'
                    : 'border-border bg-muted/50 hover:bg-muted',
                )}
              >
                {slot.label}
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export default CityGrid
