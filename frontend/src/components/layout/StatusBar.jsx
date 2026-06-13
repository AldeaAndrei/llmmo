import { Separator } from '@/components/ui/separator'

function StatusBar() {
  return (
    <div className="flex flex-wrap items-center gap-3 border-b px-4 py-2 text-sm text-muted-foreground">
      <span>Gold · Stone · Wood · Food — — —</span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Queue: —</span>
      <Separator orientation="vertical" className="hidden h-4 sm:block" />
      <span>Actions: —</span>
    </div>
  )
}

export default StatusBar
