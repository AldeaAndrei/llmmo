import { cn } from '@/lib/utils'
import BuildingImage from '@/components/buildings/BuildingImage'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'

const BUILDING_ORDER = [
  'gold_mine',
  'stone_mine',
  'timber_station',
  'bakery',
  'storage_shed',
  'barracks',
  'spy_academy',
  'wall',
]

function formatBuildingEffect(building) {
  if (building.currentEffect) {
    return building.currentEffect
  }

  if (building.productionPerTick && building.productionResource) {
    return `+${building.productionPerTick} ${building.productionResource}/tick`
  }

  return null
}

function BuildingListItem({ building, isSelected, onSelect, production }) {
  return (
    <li className="h-28">
      <button
        type="button"
        aria-label={building.name}
        title={building.name}
        onClick={() => onSelect(building.type)}
        className={cn(
          'grid h-full w-full grid-cols-[4fr_1fr] overflow-hidden rounded-md border text-left transition-colors',
          isSelected
            ? 'border-primary bg-primary/10'
            : 'border-border bg-muted/30 hover:bg-muted/60',
        )}
      >
        <div className="flex h-full min-h-0 items-center justify-center overflow-hidden p-2">
          <BuildingImage
            type={building.type}
            level={building.level}
            name={building.name}
            size="fill"
          />
        </div>
        <div className="grid h-full min-h-0 grid-rows-2 overflow-hidden border-l border-border/60">
          <div className="flex items-center justify-center px-1 text-center text-sm font-medium leading-tight">
            Lv {building.level}
          </div>
          <div className="flex items-center justify-center border-t border-border/60 px-1 text-center text-[10px] leading-tight text-muted-foreground">
            {production ?? '—'}
          </div>
        </div>
      </button>
    </li>
  )
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

  const handleSelect = (buildingType) => {
    setSelection({ type: 'building', id: buildingType })
  }

  return (
    <div className="flex h-full min-h-0 flex-col p-4">
      <div className="mb-4 shrink-0">
        <h2 className="font-medium">{primaryCity.name}</h2>
        <p className="text-sm text-muted-foreground">
          ({primaryCity.x}, {primaryCity.y}) · Troops{' '}
          {(primaryCity.troops ?? [])
            .filter((t) => t.isCombat)
            .reduce((sum, t) => sum + t.quantity, 0)}
        </p>
      </div>
      <ul className="grid min-h-0 flex-1 auto-rows-auto grid-cols-1 content-start items-start gap-2 overflow-y-auto md:grid-cols-2">
        {buildings.map((building) => (
          <BuildingListItem
            key={building.type}
            building={building}
            isSelected={
              selection?.type === 'building' && selection.id === building.type
            }
            onSelect={handleSelect}
            production={formatBuildingEffect(building)}
          />
        ))}
      </ul>
    </div>
  )
}

export default BuildingList
