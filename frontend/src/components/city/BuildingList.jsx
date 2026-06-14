import { cn } from '@/lib/utils'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'

const BUILDING_ORDER = [
  'gold_mine',
  'stone_mine',
  'timber_station',
  'bakery',
  'barracks',
]

function formatProduction(building) {
  if (!building.productionPerTick || !building.productionResource) {
    return null
  }

  const resource = building.productionResource
  return `+${building.productionPerTick} ${resource}/tick`
}

function BuildingList() {
  const { isAuthenticated } = useAuth()
  const { selection, setSelection } = useGameUI()
  const { primaryCity, loading } = useGameData()

  if (!isAuthenticated) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Log in to view your city
      </div>
    )
  }

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Loading city…
      </div>
    )
  }

  if (!primaryCity) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        No city found
      </div>
    )
  }

  const buildings = [...(primaryCity.buildings ?? [])].sort(
    (a, b) =>
      BUILDING_ORDER.indexOf(a.type) - BUILDING_ORDER.indexOf(b.type),
  )

  return (
    <div className="flex h-full flex-col p-4">
      <div className="mb-4">
        <h2 className="font-medium">{primaryCity.name}</h2>
        <p className="text-sm text-muted-foreground">
          ({primaryCity.x}, {primaryCity.y}) · Troops{' '}
          {(primaryCity.troops ?? [])
            .filter((t) => t.isCombat)
            .reduce((sum, t) => sum + t.quantity, 0)}
        </p>
      </div>
      <ul className="space-y-2">
        {buildings.map((building) => {
          const isSelected =
            selection?.type === 'building' && selection.id === building.type
          const production = formatProduction(building)

          return (
            <li key={building.type}>
              <button
                type="button"
                onClick={() =>
                  setSelection({ type: 'building', id: building.type })
                }
                className={cn(
                  'flex w-full items-center justify-between rounded-md border px-3 py-2 text-left text-sm transition-colors',
                  isSelected
                    ? 'border-primary bg-primary/10'
                    : 'border-border bg-muted/30 hover:bg-muted/60',
                )}
              >
                <span>
                  <span className="font-medium">{building.name}</span>
                  <span className="ml-2 text-muted-foreground">
                    Lv {building.level}
                  </span>
                </span>
                {production && (
                  <span className="text-xs text-muted-foreground">
                    {production}
                  </span>
                )}
              </button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}

export default BuildingList
