import { useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { useCityActions } from '@/hooks/useCityActions'

function formatCost(cost) {
  if (!cost) return null
  const parts = []
  if (cost.wood) parts.push(`${cost.wood} wood`)
  if (cost.stone) parts.push(`${cost.stone} stone`)
  if (cost.gold) parts.push(`${cost.gold} gold`)
  if (cost.food) parts.push(`${cost.food} food`)
  return parts.join(', ')
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
        <p className="text-sm text-muted-foreground">
          Upgrade cost: {formatCost(building.nextUpgradeCost)}
        </p>
      )}

      {building.canTrainTroops && building.trainCostPerTroop && (
        <p className="text-sm text-muted-foreground">
          Train {trainCount} troops: {formatCost(trainTotalCost)} (max{' '}
          {building.trainCapacity} per action)
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
