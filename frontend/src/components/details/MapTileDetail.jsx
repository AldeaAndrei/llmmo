import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import AttackTroopModal from '@/components/details/AttackTroopModal'
import CityActionsList from '@/components/details/CityActionsList'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { api } from '@/lib/api'
import { findCityAt, parseTileId } from '@/lib/map'

function formatTroops(troops) {
  if (!troops?.length) return 'none'
  return troops
    .filter((t) => t.quantity > 0)
    .map((t) => `${t.quantity} ${t.name ?? t.type}`)
    .join(', ')
}

function MapTileDetail({ selection }) {
  const { x, y } = parseTileId(selection.id)
  const { isAuthenticated } = useAuth()
  const { mapCities, playerId, primaryCity, cities, refreshCities } = useGameData()
  const [cityDetail, setCityDetail] = useState(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [attackOpen, setAttackOpen] = useState(false)
  const [scoutOpen, setScoutOpen] = useState(false)

  const cityAtTile = findCityAt(mapCities, x, y)
  const isOwnCity = cityAtTile && playerId && cityAtTile.playerId === playerId
  const ownCityFull = isOwnCity
    ? cities.find((city) => city.id === cityAtTile.id) ?? primaryCity
    : null

  const canAttack = useMemo(
    () => (primaryCity?.troops ?? []).some((t) => t.isCombat && t.quantity > 0),
    [primaryCity?.troops],
  )

  const canScout = useMemo(
    () => (primaryCity?.troops ?? []).some((t) => t.type === 'spy' && t.quantity > 0),
    [primaryCity?.troops],
  )

  useEffect(() => {
    if (!cityAtTile) {
      setCityDetail(null)
      return
    }

    if (isOwnCity) {
      setCityDetail(null)
      refreshCities()
      return
    }

    let cancelled = false
    setLoadingDetail(true)

    api
      .getCityPublic(cityAtTile.id)
      .then((detail) => {
        if (!cancelled) setCityDetail(detail)
      })
      .catch(() => {
        if (!cancelled) setCityDetail(null)
      })
      .finally(() => {
        if (!cancelled) setLoadingDetail(false)
      })

    return () => {
      cancelled = true
    }
  }, [cityAtTile, isOwnCity, refreshCities])

  const handleExpeditionSuccess = async (label) => {
    await refreshCities()
    toast.success(`${label} launched`)
  }

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Tile</h2>
        <p className="text-sm text-muted-foreground">
          ({x}, {y})
          {isOwnCity ? ' · Your city' : cityAtTile ? ' · Occupied' : ' · Empty'}
        </p>
      </div>

      {loadingDetail && (
        <p className="text-sm text-muted-foreground">Loading city…</p>
      )}

      {ownCityFull && (
        <div className="space-y-1 text-sm">
          <p className="font-medium">{ownCityFull.name}</p>
          <p className="text-muted-foreground">
            Troops {formatTroops(ownCityFull.troops)} · Wood {ownCityFull.wood} · Stone{' '}
            {ownCityFull.stone} · Gold {ownCityFull.gold} · Food {ownCityFull.food}
          </p>
        </div>
      )}

      {cityDetail && !isOwnCity && (
        <div className="space-y-1 text-sm">
          <p className="font-medium">{cityDetail.name}</p>
          {cityDetail.visibility === 'scouted' && cityDetail.resources && (
            <p className="text-muted-foreground">
              Wood {cityDetail.resources.wood} · Stone {cityDetail.resources.stone} · Gold{' '}
              {cityDetail.resources.gold} · Food {cityDetail.resources.food}
            </p>
          )}
          {cityDetail.visibility === 'scouted' && cityDetail.troops && (
            <p className="text-muted-foreground">
              Troops {cityDetail.troops.map((t) => `${t.count} ${t.type}`).join(', ')}
            </p>
          )}
          {cityDetail.visibility === 'public' && (
            <p className="text-muted-foreground">
              Enemy city — scout to reveal resources and troops.
            </p>
          )}
        </div>
      )}

      {isAuthenticated && primaryCity && !isOwnCity && (
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            disabled={!canScout}
            onClick={() => setScoutOpen(true)}
          >
            Scout tile
          </Button>
          {cityAtTile && (
            <Button
              variant="outline"
              disabled={!canAttack}
              onClick={() => setAttackOpen(true)}
            >
              Attack city
            </Button>
          )}
        </div>
      )}

      {isOwnCity && (
        <p className="text-sm text-muted-foreground">
          This is your city. Switch to the City tab to manage buildings.
        </p>
      )}

      <CityActionsList
        cityId={isOwnCity ? cityAtTile?.id : primaryCity?.id}
        title={isOwnCity ? 'City actions' : 'Your city actions'}
        ownedOnly
      />

      <AttackTroopModal
        open={scoutOpen}
        onOpenChange={setScoutOpen}
        mode="scout"
        sourceCity={primaryCity}
        targetCityId={cityAtTile?.id ?? null}
        targetX={x}
        targetY={y}
        onSuccess={() => handleExpeditionSuccess('Scout')}
      />

      <AttackTroopModal
        open={attackOpen}
        onOpenChange={setAttackOpen}
        mode="attack"
        sourceCity={primaryCity}
        targetCityId={cityAtTile?.id}
        targetX={x}
        targetY={y}
        onSuccess={() => handleExpeditionSuccess('Attack')}
      />
    </div>
  )
}

export default MapTileDetail
