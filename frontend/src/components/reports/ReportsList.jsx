import { cn } from '@/lib/utils'
import { useAuth } from '@/context/AuthContext'
import { useReports } from '@/context/ReportsContext'
import { useGameUI } from '@/context/GameUIContext'

function formatReportTitle(report) {
  const isDefense = report.payload?.perspective === 'defender'
  const typeLabel = report.type === 'scout'
    ? 'Scout'
    : isDefense
      ? 'Defense'
      : 'Attack'
  const arrow = isDefense ? '←' : '→'
  return `${typeLabel} ${arrow} (${report.targetX}, ${report.targetY})`
}

function formatReportTime(createdAt) {
  if (!createdAt) {
    return ''
  }

  return new Date(createdAt).toLocaleString()
}

function ReportsList() {
  const { isAuthenticated } = useAuth()
  const { selection, setSelection } = useGameUI()
  const { reports, loading, hasLoaded } = useReports()

  if (!isAuthenticated) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Log in to view reports
      </div>
    )
  }

  if (loading && !hasLoaded) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        Loading reports…
      </div>
    )
  }

  if (hasLoaded && reports.length === 0) {
    return (
      <div className="flex h-full items-center justify-center p-4 text-sm text-muted-foreground">
        No reports yet.
      </div>
    )
  }

  return (
    <div className="flex h-full min-h-0 flex-col p-4">
      <div className="mb-4 shrink-0">
        <h2 className="font-medium">Reports</h2>
        <p className="text-sm text-muted-foreground">
          Attack and scout outcomes
        </p>
      </div>
      <ul className="min-h-0 flex-1 space-y-2 overflow-y-auto">
        {reports.map((report) => {
          const isSelected =
            selection?.type === 'report' && selection.id === report.id
          const isUnread = !report.readAt

          return (
            <li key={report.id}>
              <button
                type="button"
                onClick={() =>
                  setSelection({ type: 'report', id: report.id })
                }
                className={cn(
                  'flex w-full items-start gap-2 rounded-md border px-3 py-2 text-left text-sm transition-colors',
                  isSelected
                    ? 'border-primary bg-primary/10'
                    : 'border-border bg-muted/30 hover:bg-muted/60',
                )}
              >
                {isUnread && (
                  <span
                    className="mt-1.5 size-2 shrink-0 rounded-full bg-destructive"
                    aria-hidden
                  />
                )}
                <span className={cn('min-w-0 flex-1', !isUnread && 'pl-4')}>
                  <span
                    className={cn(
                      'block font-medium capitalize',
                      isUnread && 'text-foreground',
                    )}
                  >
                    {formatReportTitle(report)}
                  </span>
                  <span className="mt-0.5 block text-xs text-muted-foreground">
                    {formatReportTime(report.createdAt)}
                  </span>
                </span>
              </button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}

export default ReportsList
