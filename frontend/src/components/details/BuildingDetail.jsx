import { useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useCityActions } from '@/hooks/useCityActions'

const RESOURCE_KEYS = ['wood', 'stone', 'gold', 'food']

function CostDisplay({ cost, available }) {
  if (!cost) return null

  const entries = RESOURCE_KEYS.filter((key) => cost[key]).map((key) => ({
    key,
    amount: cost[key],
    affordable: (available?.[key] ?? 0) >= cost[key],
  }))

  if (entries.length === 0) return null

  return entries.map((entry, index) => (
    <span key={entry.key}>
      {index > 0 && ', '}
      <span className={entry.affordable ? 'text-green-600' : 'text-destructive'}>
        {entry.amount} {entry.key}
      </span>
    </span>
  ))
}

function BuildingDetail({ selection }) {
  const { isAuthenticated } = useAuth()
  const { primaryCity, submitAction } = useGameData()
  const { actions } = useCityActions(primaryCity?.id)
  const [submitting, setSubmitting] = useState(false)

  const building = primaryCity?.buildings?.find(
    (b) => b.type === selection.id,
  )

  const upgradeBusy = useMemo(
    () => actions.some(
      (action) => action.status === 'in_progress' && action.type === 'upgrade',
    ),
    [actions],
  )

  const trainBusy = useMemo(
    () => actions.some(
      (action) => action.status === 'in_progress' && action.type === 'train',
    ),
    [actions],
  )

  const trainCount = useMemo(() => {
    if (!building?.trainCapacity) {
      return 5
    }

    return Math.min(5, building.trainCapacity)
  }, [building?.trainCapacity])

  const trainTotalCost = useMemo(() => {
    if (!building?.trainCostPerTroop) {
      return null
    }

    return {
      wood: building.trainCostPerTroop.wood * trainCount,
      stone: building.trainCostPerTroop.stone * trainCount,
      gold: building.trainCostPerTroop.gold * trainCount,
      food: building.trainCostPerTroop.food * trainCount,
    }
  }, [building?.trainCostPerTroop, trainCount])

  const handleUpgrade = async () => {
    setSubmitting(true)
    try {
      await submitAction('upgrade', { buildingType: building.type })
    } finally {
      setSubmitting(false)
    }
  }

  const handleTrain = async () => {
    setSubmitting(true)
    try {
      await submitAction('train', { count: trainCount, buildingType: building.type })
    } finally {
      setSubmitting(false)
    }
  }

  if (!building) {
    return (
      <div className="space-y-2">
        <h2 className="text-lg font-semibold">Building</h2>
        <p className="text-sm text-muted-foreground">
          Select a building from your city list.
        </p>
      </div>
    )
  }

  const production =
    building.productionPerTick && building.productionResource
      ? `+${building.productionPerTick} ${building.productionResource} per tick`
      : null

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{building.name}</h2>
        <p className="text-sm text-muted-foreground">Level {building.level}</p>
      </div>

      {production && (
        <p className="text-sm">Production: {production}</p>
      )}

      {building.nextUpgradeCost && (
        <p className="text-sm">
          <span className="text-muted-foreground">Upgrade cost: </span>
          <CostDisplay
            cost={building.nextUpgradeCost}
            available={primaryCity}
          />
        </p>
      )}

      {building.canTrainTroops && building.trainCostPerTroop && (
        <p className="text-sm">
          <span className="text-muted-foreground">
            Train {trainCount} troops:{' '}
          </span>
          <CostDisplay cost={trainTotalCost} available={primaryCity} />
          <span className="text-muted-foreground">
            {' '}
            (max {building.trainCapacity} per action)
          </span>
        </p>
      )}

      {isAuthenticated && primaryCity && (
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            disabled={submitting || upgradeBusy}
            onClick={handleUpgrade}
          >
            Upgrade
          </Button>
          {building.canTrainTroops && (
            <Button
              variant="outline"
              disabled={submitting || trainBusy || trainCount <= 0}
              onClick={handleTrain}
            >
              Train {trainCount} troops
            </Button>
          )}
        </div>
      )}

      <CityActionsList cityId={primaryCity?.id} ownedOnly />
    </div>
  )
}

export default BuildingDetail
