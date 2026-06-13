import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import { useGameData } from '@/context/GameDataContext'
import { api } from '@/lib/api'
import { findCityAt, parseTileId } from '@/lib/map'

function MapTileDetail({ selection }) {
  const { x, y } = parseTileId(selection.id)
  const { mapCities, playerId, primaryCity, submitAction, refreshCities } = useGameData()
  const [cityDetail, setCityDetail] = useState(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const cityAtTile = findCityAt(mapCities, x, y)
  const isOwnCity = cityAtTile && playerId && cityAtTile.playerId === playerId

  useEffect(() => {
    if (!cityAtTile) {
      setCityDetail(null)
      return
    }

    let cancelled = false
    setLoadingDetail(true)

    const isOwn = playerId && cityAtTile.playerId === playerId

    api
      .getCity(cityAtTile.id, isOwn ? playerId : undefined)
      .then((detail) => {
        if (!cancelled) {
          setCityDetail(detail)
          if (isOwn) {
            refreshCities(playerId)
          }
        }
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
  }, [cityAtTile, playerId, refreshCities])

  const handleScout = async () => {
    setSubmitting(true)
    try {
      await submitAction('scout', { targetX: x, targetY: y })
    } finally {
      setSubmitting(false)
    }
  }

  const handleAttack = async () => {
    if (!cityAtTile) return
    setSubmitting(true)
    try {
      await submitAction('attack', { targetCityId: cityAtTile.id })
    } finally {
      setSubmitting(false)
    }
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

      {cityDetail && (
        <div className="space-y-1 text-sm">
          <p className="font-medium">{cityDetail.name}</p>
          <p className="text-muted-foreground">
            Troops {cityDetail.troopCount} · Wood {cityDetail.wood} · Stone{' '}
            {cityDetail.stone} · Gold {cityDetail.gold} · Food {cityDetail.food}
          </p>
        </div>
      )}

      {!cityAtTile && primaryCity && (
        <Button
          variant="outline"
          disabled={submitting}
          onClick={handleScout}
        >
          Scout tile
        </Button>
      )}

      {cityAtTile && !isOwnCity && primaryCity && (
        <Button
          variant="outline"
          disabled={submitting}
          onClick={handleAttack}
        >
          Attack city
        </Button>
      )}

      {isOwnCity && (
        <p className="text-sm text-muted-foreground">
          This is your city. Switch to the City tab to manage buildings.
        </p>
      )}

      <CityActionsList
        cityId={cityAtTile?.id ?? primaryCity?.id}
        title={cityAtTile ? 'City actions' : 'Your city actions'}
      />
    </div>
  )
}

export default MapTileDetail
