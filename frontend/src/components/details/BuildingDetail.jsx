import { Button } from '@/components/ui/button'

function BuildingDetail({ selection }) {
  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Building</h2>
        <p className="text-sm text-muted-foreground">{selection.id}</p>
      </div>
      <p className="text-sm">Placeholder building description.</p>
      <Button variant="outline">Action placeholder</Button>
    </div>
  )
}

export default BuildingDetail
