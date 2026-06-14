import { useEffect, useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { api } from '@/lib/api'

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

function TrainTroopModal({
  open,
  onOpenChange,
  building,
  city,
  onSubmit,
  submitting,
}) {
  const [catalog, setCatalog] = useState([])
  const [troopType, setTroopType] = useState('soldier')
  const [count, setCount] = useState(1)

  useEffect(() => {
    if (!open) return

    api.getTroopCatalog().then(setCatalog).catch(() => setCatalog([]))
    setTroopType('soldier')
    setCount(1)
  }, [open])

  const trainable = useMemo(
    () =>
      catalog.filter(
        (t) => t.trainAtBuilding === 'barracks' && building?.canTrainTroops,
      ),
    [catalog, building?.canTrainTroops],
  )

  const selected = trainable.find((t) => t.type === troopType) ?? trainable[0]
  const maxCount = building?.trainCapacity ?? 1

  const totalCost = useMemo(() => {
    if (!selected) return null
    const per = selected.trainCostPerUnit
    return {
      wood: per.wood * count,
      stone: per.stone * count,
      gold: per.gold * count,
      food: per.food * count,
    }
  }, [selected, count])

  const handleSubmit = () => {
    onSubmit?.({ troopType: selected?.type ?? troopType, count })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Train troops</DialogTitle>
          <DialogDescription>
            Barracks level {building?.level} · max {maxCount} per action
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="train-type">
              Troop type
            </label>
            <select
              id="train-type"
              value={troopType}
              onChange={(e) => setTroopType(e.target.value)}
              className="w-full rounded-md border bg-background px-2 py-1.5 text-sm"
            >
              {trainable.map((t) => (
                <option key={t.type} value={t.type}>
                  {t.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="train-count">
              Count
            </label>
            <input
              id="train-count"
              type="number"
              min={1}
              max={maxCount}
              value={count}
              onChange={(e) =>
                setCount(Math.min(maxCount, Math.max(1, Number(e.target.value) || 1)))
              }
              className="w-full rounded-md border bg-background px-2 py-1.5 text-sm"
            />
          </div>

          {totalCost && (
            <p className="text-sm">
              <span className="text-muted-foreground">Cost: </span>
              <CostDisplay cost={totalCost} available={city} />
            </p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting || !selected}>
            Train {count} {selected?.name ?? 'troops'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default TrainTroopModal
