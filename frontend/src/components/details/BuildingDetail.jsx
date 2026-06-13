import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useGameData } from '@/context/GameDataContext'

function BuildingDetail({ selection }) {
  const { primaryCity, submitAction } = useGameData()
  const [submitting, setSubmitting] = useState(false)

  const handleTrain = async () => {
    setSubmitting(true)
    try {
      await submitAction('train', { count: 5, slot: selection.id })
    } finally {
      setSubmitting(false)
    }
  }

  const handleBuild = async () => {
    setSubmitting(true)
    try {
      await submitAction('build', { slot: selection.id })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Building slot</h2>
        <p className="text-sm text-muted-foreground">{selection.id}</p>
      </div>
      <p className="text-sm text-muted-foreground">
        Queue build or train actions for this slot. Building types are not
        modeled yet — actions are stored for the tick worker.
      </p>
      {primaryCity && (
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            disabled={submitting}
            onClick={handleBuild}
          >
            Build
          </Button>
          <Button
            variant="outline"
            disabled={submitting}
            onClick={handleTrain}
          >
            Train 5 troops
          </Button>
        </div>
      )}
    </div>
  )
}

export default BuildingDetail
