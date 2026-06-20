import { useEffect, useMemo, useRef, useState } from 'react'
import { toast } from 'sonner'
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
import { useTickTime } from '@/hooks/useTickTime'

function carryCapacity(troop) {
  return (
    (troop.capacityWood ?? 0) +
    (troop.capacityStone ?? 0) +
    (troop.capacityGold ?? 0) +
    (troop.capacityFood ?? 0)
  )
}

function TroopPartyCard({
  troop,
  count,
  onCountChange,
  sendLabel = 'Send count',
  readOnly = false,
}) {
  const maxAvailable = troop.quantity ?? 0
  const countNum = Math.max(0, Number.parseInt(count ?? '', 10) || 0)
  const hasSelection = countNum > 0

  return (
    <div
      className={`rounded-md border p-3 space-y-2 ${
        hasSelection ? 'border-primary bg-primary/5' : 'border-border'
      }`}
    >
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h3 className="font-medium capitalize">{troop.name}</h3>
        <p className="text-xs text-muted-foreground font-mono">
          melee {troop.attackMelee} · range {troop.attackRange} · speed{' '}
          {troop.speed} · carry {carryCapacity(troop)}
        </p>
      </div>

      <p className="text-sm text-muted-foreground">
        <span className="text-foreground">Available: </span>
        {maxAvailable}
      </p>

      <div className="flex items-center gap-2">
        <label
          className="text-sm text-muted-foreground shrink-0"
          htmlFor={`party-count-${troop.type}`}
        >
          {sendLabel}
        </label>
        <input
          id={`party-count-${troop.type}`}
          type="number"
          min={0}
          max={maxAvailable}
          value={count}
          readOnly={readOnly}
          disabled={readOnly}
          onChange={(e) => onCountChange(troop.type, e.target.value)}
          className="w-24 rounded-md border bg-background px-2 py-1.5 text-sm disabled:opacity-70"
        />
        <span className="text-xs text-muted-foreground">max {maxAvailable}</span>
      </div>
    </div>
  )
}

function AttackTroopModal({
  open,
  onOpenChange,
  mode,
  sourceCity,
  targetCityId,
  targetX,
  targetY,
  onSuccess,
}) {
  const prevOpenRef = useRef(false)
  const sourceCityRef = useRef(sourceCity)
  const targetRef = useRef({ targetCityId, targetX, targetY })
  sourceCityRef.current = sourceCity
  targetRef.current = { targetCityId, targetX, targetY }

  const [session, setSession] = useState(null)
  const [counts, setCounts] = useState({})
  const [preview, setPreview] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const { formatTicksAsDuration } = useTickTime()

  const isScout = mode === 'scout'

  // Snapshot party + target once when the modal opens — not on every poll.
  useEffect(() => {
    const justOpened = open && !prevOpenRef.current
    prevOpenRef.current = open

    if (!open) {
      setSession(null)
      setCounts({})
      setPreview(null)
      return
    }

    if (!justOpened) {
      return
    }

    const snapshotCity = sourceCityRef.current
    const snapshotTarget = targetRef.current
    if (!snapshotCity?.id) {
      return
    }

    const combatTroops = (snapshotCity.troops ?? []).filter(
      (t) => t.isCombat && t.quantity > 0,
    )
    const spyTroop = (snapshotCity.troops ?? []).find((t) => t.type === 'spy')

    const troops = isScout
      ? spyTroop && spyTroop.quantity > 0
        ? [spyTroop]
        : []
      : combatTroops

    setSession({
      sourceCityId: snapshotCity.id,
      targetCityId: snapshotTarget.targetCityId ?? null,
      targetX: snapshotTarget.targetX,
      targetY: snapshotTarget.targetY,
      troops,
    })

    const initial = {}
    for (const troop of troops) {
      initial[troop.type] = isScout ? '1' : ''
    }
    setCounts(initial)
    setPreview(null)
  }, [open, isScout])

  const troopsPayload = useMemo(() => {
    if (!session) {
      return []
    }

    if (isScout) {
      return [{ type: 'spy', count: 1 }]
    }

    return session.troops
      .map((troop) => ({
        type: troop.type,
        count: Math.max(0, Number.parseInt(counts[troop.type] ?? '', 10) || 0),
      }))
      .filter((entry) => entry.count > 0)
  }, [session, isScout, counts])

  const partySize = troopsPayload.reduce((sum, entry) => sum + entry.count, 0)

  useEffect(() => {
    if (!open || !session?.sourceCityId || troopsPayload.length === 0) {
      setPreview(null)
      return
    }

    let cancelled = false

    api
      .previewAttack({
        sourceCityId: session.sourceCityId,
        targetCityId: session.targetCityId,
        targetX: session.targetX,
        targetY: session.targetY,
        type: mode,
        troops: troopsPayload,
      })
      .then((data) => {
        if (!cancelled) setPreview(data)
      })
      .catch(() => {
        if (!cancelled) setPreview(null)
      })

    return () => {
      cancelled = true
    }
  }, [open, session, mode, troopsPayload])

  const handleCountChange = (type, rawValue) => {
    const troop = session?.troops.find((t) => t.type === type)
    const maxAvailable = troop?.quantity ?? 0

    if (rawValue === '') {
      setCounts((prev) => ({ ...prev, [type]: '' }))
      return
    }

    const parsed = Number.parseInt(rawValue, 10)
    if (Number.isNaN(parsed)) {
      return
    }

    const clamped = Math.min(maxAvailable, Math.max(0, parsed))
    setCounts((prev) => ({ ...prev, [type]: String(clamped) }))
  }

  const handleSubmit = async () => {
    if (!session?.sourceCityId || troopsPayload.length === 0) {
      return
    }

    setSubmitting(true)
    try {
      await api.createAttack({
        sourceCityId: session.sourceCityId,
        targetCityId: session.targetCityId,
        targetX: session.targetX,
        targetY: session.targetY,
        type: mode,
        troops: troopsPayload,
      })
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      toast.error(err.message ?? 'Failed to launch expedition')
    } finally {
      setSubmitting(false)
    }
  }

  const totalTravelTicks =
    preview?.valid && preview.outboundTicks != null
      ? isScout
        ? preview.outboundTicks
        : preview.outboundTicks + (preview.returnTicks ?? 0)
      : null

  const canSubmit = preview?.valid === true && !submitting && partySize > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isScout ? 'Scout tile' : 'Attack city'}</DialogTitle>
          <DialogDescription>
            Target ({session?.targetX ?? targetX}, {session?.targetY ?? targetY})
            {preview?.valid && preview.manhattan != null && (
              <>
                {' '}
                · {preview.manhattan} tile{preview.manhattan === 1 ? '' : 's'}
              </>
            )}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          {session?.troops.map((troop) => (
            <TroopPartyCard
              key={troop.type}
              troop={troop}
              count={counts[troop.type] ?? ''}
              onCountChange={handleCountChange}
              sendLabel={isScout ? 'Scouts to send' : 'Send count'}
              readOnly={isScout}
            />
          ))}

          {session && session.troops.length === 0 && (
            <p className="text-sm text-muted-foreground">
              {isScout
                ? 'No spies available to scout.'
                : 'No combat troops available to send.'}
            </p>
          )}

          {preview?.errors?.length > 0 && partySize > 0 && (
            <ul className="space-y-1 text-sm text-destructive">
              {preview.errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          )}

          {partySize > 0 && preview && (
            <div className="rounded-md border bg-muted/30 p-3 text-sm space-y-1">
              {preview.partySpeed != null && (
                <p>
                  <span className="text-muted-foreground">Party speed: </span>
                  {preview.partySpeed}
                </p>
              )}
              {preview.outboundTicks != null && (
                <p>
                  <span className="text-muted-foreground">Outbound: </span>
                  {formatTicksAsDuration(preview.outboundTicks)}
                </p>
              )}
              {!isScout && preview.returnTicks != null && (
                <p>
                  <span className="text-muted-foreground">Return: </span>
                  {formatTicksAsDuration(preview.returnTicks)}
                </p>
              )}
              {totalTravelTicks != null && (
                <p className="font-medium">
                  <span className="text-muted-foreground font-normal">
                    Total {isScout ? 'travel' : 'attack'} time:{' '}
                  </span>
                  {formatTicksAsDuration(totalTravelTicks)}
                </p>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit}>
            {isScout ? 'Send spy' : 'Launch attack'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default AttackTroopModal
