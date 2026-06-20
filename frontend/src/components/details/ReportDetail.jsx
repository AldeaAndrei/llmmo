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

function formatTroopCasualtyLine(troops) {
  if (!troops?.length) {
    return null
  }

  return troops
    .map((troop) => {
      const remaining = troop.remaining ?? troop.count ?? troop.quantity ?? 0
      const lost = troop.lost ?? 0
      if (lost > 0) {
        return `${troop.type} ${remaining} (-${lost})`
      }
      return `${troop.type} ${remaining}`
    })
    .join(' | ')
}

function formatDefenderPower(defender) {
  if (!defender) {
    return null
  }

  const total = defender.totalPower ?? defender.total
  const troopPower = defender.troopPower ?? 0
  const wallBonus = defender.wallBonus ?? 0

  if (total == null) {
    return null
  }

  if (wallBonus > 0) {
    return `Defence power ${total} (${troopPower} troops + ${wallBonus} wall)`
  }

  return `Defence power ${total} (${troopPower} troops)`
}

function formatAttackerPower(attacker) {
  if (!attacker || attacker.totalPower == null) {
    return null
  }

  return `Attack power ${attacker.totalPower}`
}

function CombatSideSection({ title, powerLine, troops }) {
  const casualtyLine = formatTroopCasualtyLine(troops)
  if (!powerLine && !casualtyLine) {
    return null
  }

  return (
    <PayloadSection title={title}>
      {powerLine && <p>{powerLine}</p>}
      {casualtyLine && <p className="font-mono text-xs">{casualtyLine}</p>}
    </PayloadSection>
  )
}

function AttackPayload({ payload }) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const isDefense = payload.perspective === 'defender'
  const outcome =
    payload.outcome === 'victory' ? 'Victory' : 'Defeat'

  const hasNewFormat = payload.attacker || payload.defender

  if (hasNewFormat) {
    return (
      <div className="space-y-3">
        <p className="text-sm font-medium">{outcome}</p>

        <CombatSideSection
          title="Attacker"
          powerLine={formatAttackerPower(payload.attacker)}
          troops={payload.attacker?.troops}
        />

        <CombatSideSection
          title={isDefense ? 'Defender (you)' : 'Defender'}
          powerLine={formatDefenderPower(payload.defender)}
          troops={payload.defender?.troops}
        />

        {payload.loot &&
          (payload.loot.wood > 0 ||
            payload.loot.stone > 0 ||
            payload.loot.gold > 0 ||
            payload.loot.food > 0) && (
          <PayloadSection title={isDefense ? 'Resources lost' : 'Loot'}>
            <p>{formatResourceLine(payload.loot)}</p>
          </PayloadSection>
        )}
      </div>
    )
  }

  // Legacy reports (before structured troop snapshots)
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
        <PayloadSection title="Attacking force">
          <TroopLines troops={payload.committed} />
        </PayloadSection>
      )}
      {payload.attackerCasualties?.length > 0 && (
        <PayloadSection title="Attacker casualties">
          <TroopLines troops={payload.attackerCasualties} />
        </PayloadSection>
      )}
      {payload.defenderCasualties && (
        <PayloadSection title={isDefense ? 'Your casualties' : 'Defender casualties'}>
          {formatCasualties(payload.defenderCasualties)}
        </PayloadSection>
      )}
      {!isDefense && payload.survivors?.length > 0 && (
        <PayloadSection title="Survivors">
          <TroopLines troops={payload.survivors} />
        </PayloadSection>
      )}
      {payload.loot && (
        <PayloadSection title={isDefense ? 'Resources lost' : 'Loot'}>
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

  const isDefense = report.payload?.perspective === 'defender'
  const typeLabel =
    report.type === 'scout'
      ? 'Scout report'
      : isDefense
        ? 'Defense report'
        : 'Attack report'

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{typeLabel}</h2>
        <p className="text-sm text-muted-foreground">
          {isDefense ? 'Attacked from' : 'Target'}{' '}
          {formatLocation(report.targetX, report.targetY)}
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
