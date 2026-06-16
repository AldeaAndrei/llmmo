import { useEffect } from 'react'
import { useReports } from '@/context/ReportsContext'
import { formatResourceLine } from '@/lib/reportResources'

function formatLocation(x, y) {
  return `(${x}, ${y})`
}

function PayloadSection({ title, children }) {
  if (!children) {
    return null
  }

  return (
    <div className="space-y-1">
      <h3 className="text-sm font-medium">{title}</h3>
      <div className="text-sm text-muted-foreground">{children}</div>
    </div>
  )
}

function TroopLines({ troops, label = 'type' }) {
  if (!troops?.length) {
    return null
  }

  return (
    <ul className="space-y-0.5">
      {troops.map((troop) => (
        <li key={`${troop.type}-${troop.quantity ?? troop.count}`}>
          {troop.type}: {troop.quantity ?? troop.count}
        </li>
      ))}
    </ul>
  )
}

function ScoutPayload({ payload }) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const result = payload.result === 'intel' ? 'Intel gathered' : 'Nothing found'

  return (
    <div className="space-y-3">
      <p className="text-sm">{result}</p>
      {payload.spySurvived != null && (
        <p className="text-sm text-muted-foreground">
          Spy {payload.spySurvived ? 'survived' : 'died'}
        </p>
      )}
      {payload.resources && (
        <PayloadSection title="Resources">
          <p>{formatResourceLine(payload.resources)}</p>
        </PayloadSection>
      )}
      {payload.troops?.length > 0 && (
        <PayloadSection title="Troops">
          <TroopLines troops={payload.troops} />
        </PayloadSection>
      )}
    </div>
  )
}

function formatCasualties(casualties) {
  if (!casualties) {
    return null
  }

  if (Array.isArray(casualties)) {
    return <TroopLines troops={casualties} />
  }

  const entries = Object.entries(casualties)
  if (entries.length === 0) {
    return null
  }

  return (
    <ul className="space-y-0.5">
      {entries.map(([type, count]) => (
        <li key={type}>
          {type}: {count}
        </li>
      ))}
    </ul>
  )
}

function AttackPayload({ payload }) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const outcome =
    payload.outcome === 'victory' ? 'Victory' : 'Defeat'

  return (
    <div className="space-y-3">
      <p className="text-sm font-medium">{outcome}</p>
      {(payload.attackerPower != null || payload.defenderPower != null) && (
        <p className="text-sm text-muted-foreground">
          Attacker power {payload.attackerPower ?? '—'} · Defender power{' '}
          {payload.defenderPower ?? '—'}
        </p>
      )}
      {payload.committed?.length > 0 && (
        <PayloadSection title="Committed">
          <TroopLines troops={payload.committed} />
        </PayloadSection>
      )}
      {payload.attackerCasualties?.length > 0 && (
        <PayloadSection title="Attacker casualties">
          <TroopLines troops={payload.attackerCasualties} />
        </PayloadSection>
      )}
      {payload.defenderCasualties && (
        <PayloadSection title="Defender casualties">
          {formatCasualties(payload.defenderCasualties)}
        </PayloadSection>
      )}
      {payload.survivors?.length > 0 && (
        <PayloadSection title="Survivors">
          <TroopLines troops={payload.survivors} />
        </PayloadSection>
      )}
      {payload.loot && (
        <PayloadSection title="Loot">
          <p>{formatResourceLine(payload.loot)}</p>
        </PayloadSection>
      )}
    </div>
  )
}

function ReportDetail({ selection }) {
  const { reports, markReportRead } = useReports()
  const report = reports.find((item) => item.id === selection.id)

  useEffect(() => {
    if (report && !report.readAt) {
      markReportRead(report.id)
    }
  }, [report, markReportRead])

  if (!report) {
    return (
      <div className="space-y-2">
        <h2 className="text-lg font-semibold">Report</h2>
        <p className="text-sm text-muted-foreground">
          Select a report from the list.
        </p>
      </div>
    )
  }

  const typeLabel = report.type === 'scout' ? 'Scout report' : 'Attack report'

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{typeLabel}</h2>
        <p className="text-sm text-muted-foreground">
          Target {formatLocation(report.targetX, report.targetY)}
        </p>
        <p className="text-sm text-muted-foreground">
          {new Date(report.createdAt).toLocaleString()}
        </p>
      </div>

      {report.type === 'scout' ? (
        <ScoutPayload payload={report.payload} />
      ) : (
        <AttackPayload payload={report.payload} />
      )}
    </div>
  )
}

export default ReportDetail
