import { useEffect, useMemo, useState } from 'react'
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
  const [counts, setCounts] = useState({})
  const [preview, setPreview] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  const combatTroops = useMemo(
    () => (sourceCity?.troops ?? []).filter((t) => t.isCombat && t.quantity > 0),
    [sourceCity?.troops],
  )

  const spyTroop = useMemo(
    () => (sourceCity?.troops ?? []).find((t) => t.type === 'spy'),
    [sourceCity?.troops],
  )

  useEffect(() => {
    if (!open) return

    if (mode === 'scout') {
      setCounts({ spy: 1 })
    } else {
      const initial = {}
      for (const troop of combatTroops) {
        initial[troop.type] = 0
      }
      setCounts(initial)
    }
    setPreview(null)
  }, [open, mode, combatTroops])

  const troopsPayload = useMemo(() => {
    if (mode === 'scout') {
      return [{ type: 'spy', count: 1 }]
    }

    return Object.entries(counts)
      .filter(([, count]) => count > 0)
      .map(([type, count]) => ({ type, count: Number(count) }))
  }, [mode, counts])

  useEffect(() => {
    if (!open || !sourceCity?.id || troopsPayload.length === 0) {
      setPreview(null)
      return
    }

    let cancelled = false

    api
      .previewAttack({
        sourceCityId: sourceCity.id,
        targetCityId: targetCityId ?? null,
        targetX,
        targetY,
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
  }, [open, sourceCity?.id, targetCityId, targetX, targetY, mode, troopsPayload])

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      await api.createAttack({
        sourceCityId: sourceCity.id,
        targetCityId: targetCityId ?? null,
        targetX,
        targetY,
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

  const canSubmit = preview?.valid === true && !submitting
  const hasSpy = (spyTroop?.quantity ?? 0) >= 1

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{mode === 'scout' ? 'Scout tile' : 'Attack city'}</DialogTitle>
          <DialogDescription>
            Target ({targetX}, {targetY})
            {preview?.valid && (
              <>
                {' '}
                · {preview.manhattan} tiles · {preview.outboundTicks} tick
                {preview.outboundTicks === 1 ? '' : 's'} outbound
              </>
            )}
          </DialogDescription>
        </DialogHeader>

        {mode === 'scout' ? (
          <p className="text-sm">
            Send 1 spy (available: {spyTroop?.quantity ?? 0})
          </p>
        ) : (
          <div className="space-y-3">
            {combatTroops.map((troop) => (
              <div key={troop.type} className="flex items-center justify-between gap-2">
                <label className="text-sm capitalize" htmlFor={`troop-${troop.type}`}>
                  {troop.name} ({troop.quantity} available)
                </label>
                <input
                  id={`troop-${troop.type}`}
                  type="number"
                  min={0}
                  max={troop.quantity}
                  value={counts[troop.type] ?? 0}
                  onChange={(e) =>
                    setCounts((prev) => ({
                      ...prev,
                      [troop.type]: Math.min(
                        troop.quantity,
                        Math.max(0, Number(e.target.value) || 0),
                      ),
                    }))
                  }
                  className="w-20 rounded-md border bg-background px-2 py-1 text-sm"
                />
              </div>
            ))}
          </div>
        )}

        {preview?.errors?.length > 0 && (
          <ul className="space-y-1 text-sm text-destructive">
            {preview.errors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!canSubmit || (mode === 'scout' && !hasSpy)}
          >
            {mode === 'scout' ? 'Send spy' : 'Launch attack'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default AttackTroopModal
