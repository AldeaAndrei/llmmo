import { Button } from '@/components/ui/button'

function KeyRevealDialog({ apiKey, title, onClose }) {
  if (!apiKey) {
    return null
  }

  const handleCopy = async () => {
    await navigator.clipboard.writeText(apiKey)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-md space-y-4 rounded-lg border bg-background p-6 shadow-lg">
        <div>
          <h2 className="text-lg font-semibold">{title}</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Copy this API key now. It will not be shown again.
          </p>
        </div>
        <code className="block break-all rounded-md bg-muted p-3 text-xs">{apiKey}</code>
        <div className="flex gap-2">
          <Button type="button" variant="outline" className="flex-1" onClick={handleCopy}>
            Copy key
          </Button>
          <Button type="button" className="flex-1" onClick={onClose}>
            Done
          </Button>
        </div>
      </div>
    </div>
  )
}

export default KeyRevealDialog
