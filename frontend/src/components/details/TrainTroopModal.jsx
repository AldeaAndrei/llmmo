import { useEffect, useMemo, useRef, useState } from 'react'
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

function formatUnitCost(cost) {
  if (!cost) return '—'
  const parts = RESOURCE_KEYS.filter((key) => cost[key]).map(
    (key) => `${cost[key]} ${key}`,
  )
  return parts.length > 0 ? parts.join(', ') : 'free'
}

function TroopTrainCard({
  troop,
  line,
  count,
  maxCapacity,
  selected,
  onSelect,
  onCountChange,
}) {
  const unitCost = troop.trainCostPerUnit
  const carry =
    troop.capacityWood +
    troop.capacityStone +
    troop.capacityGold +
    troop.capacityFood

  const lineCost = line?.cost ?? unitCost
  const duration =
    line && count > 0 ? line.durationTicks : null

  return (
    <div
      className={`rounded-md border p-3 space-y-2 ${
        selected ? 'border-primary bg-primary/5' : 'border-border'
      }`}
    >
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h3 className="font-medium capitalize">{troop.name}</h3>
        <p className="text-xs text-muted-foreground font-mono">
          melee {troop.attackMelee} · range {troop.attackRange} · speed{' '}
          {troop.speed} · carry {carry}
        </p>
      </div>

      <p className="text-sm text-muted-foreground">
        <span className="text-foreground">Cost: </span>
        {count > 0 ? formatUnitCost(lineCost) : `${formatUnitCost(unitCost)} / unit`}
        {duration != null && (
          <>
            {' '}
            · <span className="text-foreground">Train time: </span>
            {duration} tick{duration === 1 ? '' : 's'}
          </>
        )}
      </p>

      <div className="flex items-center gap-2">
        <label
          className="text-sm text-muted-foreground shrink-0"
          htmlFor={`train-count-${troop.type}`}
        >
          Train count
        </label>
        <input
          id={`train-count-${troop.type}`}
          type="number"
          min={0}
          max={maxCapacity}
          value={count}
          onFocus={() => onSelect(troop.type)}
          onChange={(e) => onCountChange(troop.type, e.target.value)}
          className="w-24 rounded-md border bg-background px-2 py-1.5 text-sm"
        />
        <span className="text-xs text-muted-foreground">max {maxCapacity}</span>
      </div>
    </div>
  )
}

function TrainTroopModal({
  open,
  onOpenChange,
  building,
  city,
  onSubmit,
  submitting,
}) {
  const prevOpenRef = useRef(false)
  const buildingRef = useRef(building)
  const cityRef = useRef(city)
  buildingRef.current = building
  cityRef.current = city

  const [catalog, setCatalog] = useState([])
  const [counts, setCounts] = useState({})
  const [selectedType, setSelectedType] = useState(null)
  const [sessionCapacity, setSessionCapacity] = useState(1)
  const [sessionResources, setSessionResources] = useState(null)
  const [preview, setPreview] = useState(null)
  const [cityId, setCityId] = useState(null)

  const trainable = useMemo(
    () => catalog.filter((t) => t.trainAtBuilding === 'barracks'),
    [catalog],
  )

  // Initialise once when the modal opens — not on every poll refresh.
  useEffect(() => {
    const justOpened = open && !prevOpenRef.current
    prevOpenRef.current = open

    if (!open) {
      setCatalog([])
      setCounts({})
      setSelectedType(null)
      setSessionCapacity(1)
      setSessionResources(null)
      setPreview(null)
      setCityId(null)
      return
    }

    if (!justOpened) {
      return
    }

    const snapshotBuilding = buildingRef.current
    const snapshotCity = cityRef.current

    setSessionCapacity(snapshotBuilding?.trainCapacity ?? 1)
    setSessionResources({
      wood: snapshotCity?.wood ?? 0,
      stone: snapshotCity?.stone ?? 0,
      gold: snapshotCity?.gold ?? 0,
      food: snapshotCity?.food ?? 0,
    })
    setCityId(snapshotCity?.id ?? null)

    api
      .getTroopCatalog()
      .then((items) => {
        const troops = items.filter((t) => t.trainAtBuilding === 'barracks')
        setCatalog(troops)
        const initial = {}
        for (const troop of troops) {
          initial[troop.type] = ''
        }
        setCounts(initial)
        setSelectedType(troops[0]?.type ?? null)
      })
      .catch(() => {
        setCatalog([])
        setCounts({})
      })
  }, [open])

  const linesPayload = useMemo(
    () =>
      trainable.map((troop) => ({
        type: troop.type,
        count: Math.max(0, Number.parseInt(counts[troop.type] ?? '', 10) || 0),
      })),
    [trainable, counts],
  )

  useEffect(() => {
    if (!open || !cityId || trainable.length === 0) {
      setPreview(null)
      return
    }

    let cancelled = false

    api
      .previewTrain({ cityId, lines: linesPayload })
      .then((data) => {
        if (!cancelled) setPreview(data)
      })
      .catch(() => {
        if (!cancelled) setPreview(null)
      })

    return () => {
      cancelled = true
    }
  }, [open, cityId, linesPayload, trainable.length])

  const handleCountChange = (type, rawValue) => {
    setSelectedType(type)
    setCounts((prev) => {
      const next = { ...prev }
      for (const key of Object.keys(next)) {
        next[key] = key === type ? rawValue : ''
      }
      return next
    })
  }

  const selectedLine = preview?.lines?.find((line) => line.type === selectedType)
  const selectedCount = Math.max(
    0,
    Number.parseInt(counts[selectedType] ?? '', 10) || 0,
  )

  const handleSubmit = () => {
    if (!selectedType || selectedCount <= 0) {
      return
    }

    onSubmit?.({ troopType: selectedType, count: selectedCount })
  }

  const canSubmit =
    preview?.valid === true && selectedCount > 0 && !submitting

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Train troops</DialogTitle>
          <DialogDescription>
            Barracks level {building?.level} · max {sessionCapacity} per action
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          {trainable.map((troop) => {
            const line = preview?.lines?.find((entry) => entry.type === troop.type)
            const countStr = counts[troop.type] ?? ''
            const countNum = Math.max(0, Number.parseInt(countStr, 10) || 0)

            return (
              <TroopTrainCard
                key={troop.type}
                troop={troop}
                line={line}
                count={countStr}
                maxCapacity={sessionCapacity}
                selected={selectedType === troop.type && countNum > 0}
                onSelect={setSelectedType}
                onCountChange={handleCountChange}
              />
            )
          })}

          {trainable.length === 0 && (
            <p className="text-sm text-muted-foreground">
              No trainable troop types at this barracks.
            </p>
          )}

          {preview?.errors?.length > 0 && selectedCount > 0 && (
            <ul className="space-y-1 text-sm text-destructive">
              {preview.errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          )}

          {selectedCount > 0 && (
            <div className="rounded-md border bg-muted/30 p-3 text-sm space-y-1">
              <p>
                <span className="text-muted-foreground">Total cost: </span>
                <CostDisplay
                  cost={preview?.totalCost}
                  available={sessionResources}
                />
              </p>
              <p>
                <span className="text-muted-foreground">Total train time: </span>
                {preview?.totalDurationTicks ?? '—'} tick
                {preview?.totalDurationTicks === 1 ? '' : 's'}
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit}>
            Train {selectedCount > 0 ? selectedCount : ''}{' '}
            {selectedLine?.name ?? 'troops'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default TrainTroopModal
