import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import CityActionsList from '@/components/details/CityActionsList'
import { useAuth } from '@/context/AuthContext'
import { useGameData } from '@/context/GameDataContext'
import { api } from '@/lib/api'
import { findCityAt, parseTileId } from '@/lib/map'

function MapTileDetail({ selection }) {
  const { x, y } = parseTileId(selection.id)
  const { isAuthenticated } = useAuth()
  const { mapCities, playerId, primaryCity, cities, submitAction, refreshCities } =
    useGameData()
  const [cityPublic, setCityPublic] = useState(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const cityAtTile = findCityAt(mapCities, x, y)
  const isOwnCity = cityAtTile && playerId && cityAtTile.playerId === playerId
  const ownCityFull = isOwnCity
    ? cities.find((city) => city.id === cityAtTile.id) ?? primaryCity
    : null

  useEffect(() => {
    if (!cityAtTile) {
      setCityPublic(null)
      return
    }

    if (isOwnCity) {
      setCityPublic(null)
      refreshCities()
      return
    }

    let cancelled = false
    setLoadingDetail(true)

    api
      .getCityPublic(cityAtTile.id)
      .then((detail) => {
        if (!cancelled) setCityPublic(detail)
      })
      .catch(() => {
        if (!cancelled) setCityPublic(null)
      })
      .finally(() => {
        if (!cancelled) setLoadingDetail(false)
      })

    return () => {
      cancelled = true
    }
  }, [cityAtTile, isOwnCity, refreshCities])

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

      {ownCityFull && (
        <div className="space-y-1 text-sm">
          <p className="font-medium">{ownCityFull.name}</p>
          <p className="text-muted-foreground">
            Troops {ownCityFull.troopCount} · Wood {ownCityFull.wood} · Stone{' '}
            {ownCityFull.stone} · Gold {ownCityFull.gold} · Food {ownCityFull.food}
          </p>
        </div>
      )}

      {cityPublic && !isOwnCity && (
        <div className="space-y-1 text-sm">
          <p className="font-medium">{cityPublic.name}</p>
          <p className="text-muted-foreground">
            Enemy city — resources hidden until scouted in-game.
          </p>
        </div>
      )}

      {!cityAtTile && isAuthenticated && primaryCity && (
        <Button
          variant="outline"
          disabled={submitting}
          onClick={handleScout}
        >
          Scout tile
        </Button>
      )}

      {cityAtTile && !isOwnCity && isAuthenticated && primaryCity && (
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
        cityId={isOwnCity ? cityAtTile?.id : primaryCity?.id}
        title={isOwnCity ? 'City actions' : 'Your city actions'}
        ownedOnly
      />
    </div>
  )
}

export default MapTileDetail
