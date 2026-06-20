import { useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import TroopMovementsList from '@/components/details/TroopMovementsList'
import TrainTroopModal from '@/components/details/TrainTroopModal'
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
  const [trainOpen, setTrainOpen] = useState(false)

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

  const handleUpgrade = async () => {
    setSubmitting(true)
    try {
      await submitAction('upgrade', { buildingType: building.type })
    } finally {
      setSubmitting(false)
    }
  }

  const handleTrain = async ({ troopType, count }) => {
    setSubmitting(true)
    try {
      await submitAction('train', {
        count,
        troopType,
        buildingType: building.type,
      })
      setTrainOpen(false)
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

  return (
    <>
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{building.name}</h2>
        <p className="text-sm text-muted-foreground">Level {building.level}</p>
        {building.description && (
          <p className="mt-1 text-sm text-muted-foreground">{building.description}</p>
        )}
      </div>

      {building.currentEffect && (
        <p className="text-sm">
          <span className="text-muted-foreground">Current effect: </span>
          {building.currentEffect}
        </p>
      )}

      {building.nextLevelEffect && (
        <p className="text-sm">
          <span className="text-muted-foreground">Next level: </span>
          {building.nextLevelEffect}
        </p>
      )}

      {building.upgradeDurationTicks != null && (
        <p className="text-sm text-muted-foreground">
          Upgrade time: {building.upgradeDurationTicks} tick
          {building.upgradeDurationTicks === 1 ? '' : 's'}
        </p>
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
              disabled={submitting || trainBusy}
              onClick={() => setTrainOpen(true)}
            >
              Train troops
            </Button>
          )}
        </div>
      )}

      <CityActionsList cityId={primaryCity?.id} ownedOnly />

      <TroopMovementsList cityId={primaryCity?.id} ownedOnly />
    </div>

      <TrainTroopModal
        open={trainOpen}
        onOpenChange={setTrainOpen}
        building={building}
        city={primaryCity}
        onSubmit={handleTrain}
        submitting={submitting}
      />
    </>
  )
}

export default BuildingDetail
