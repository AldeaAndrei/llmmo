import { useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'

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
  const [submitting, setSubmitting] = useState(false)

  const building = primaryCity?.buildings?.find(
    (b) => b.type === selection.id,
  )

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
      await submitAction('train', { count: 5, buildingType: building.type })
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
          Next upgrade: {formatCost(building.nextUpgradeCost)}
        </p>
      )}

      {isAuthenticated && primaryCity && (
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            disabled={submitting}
            onClick={handleUpgrade}
          >
            Upgrade
          </Button>
          {building.canTrainTroops && (
            <Button
              variant="outline"
              disabled={submitting}
              onClick={handleTrain}
            >
              Train 5 troops
            </Button>
          )}
        </div>
      )}

      <CityActionsList cityId={primaryCity?.id} ownedOnly />
    </div>
  )
}

export default BuildingDetail
