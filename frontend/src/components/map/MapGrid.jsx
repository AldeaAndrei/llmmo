import { useCallback, useEffect, useRef, useState } from 'react'
import { TransformWrapper, TransformComponent, useControls } from 'react-zoom-pan-pinch'
import { Button } from '@/components/ui/button'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'
import { useWorld } from '@/context/WorldContext'
import {
  buildTerrainLayer,
  citySpriteIndex,
  drawGrid,
} from '@/lib/mapTerrain'
import { loadMapSprites } from '@/lib/mapSprites'
import { api } from '@/lib/api'
import {
  CELL_SIZE,
  parseTileId,
  tileId,
} from '@/lib/map'

function drawMap({
  canvas,
  sprites,
  terrainLayer,
  mapSize,
  selectedTile,
  mapCities,
  attacks,
}) {
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const mapWidth = mapSize * CELL_SIZE
  const mapHeight = mapSize * CELL_SIZE

  ctx.clearRect(0, 0, mapWidth, mapHeight)

  if (terrainLayer) {
    ctx.drawImage(terrainLayer, 0, 0)
  }

  drawGrid(ctx, mapSize, CELL_SIZE)

  for (const attack of attacks ?? []) {
    const sx = attack.source.x * CELL_SIZE + CELL_SIZE / 2
    const sy = attack.source.y * CELL_SIZE + CELL_SIZE / 2
    const tx = attack.target.x * CELL_SIZE + CELL_SIZE / 2
    const ty = attack.target.y * CELL_SIZE + CELL_SIZE / 2

    ctx.strokeStyle = attack.type === 'scout' ? 'rgba(147, 51, 234, 0.5)' : 'rgba(220, 38, 38, 0.5)'
    ctx.lineWidth = 2
    ctx.beginPath()
    ctx.moveTo(sx, sy)
    ctx.lineTo(tx, ty)
    ctx.stroke()

    const dotX = attack.currentX * CELL_SIZE + CELL_SIZE / 2
    const dotY = attack.currentY * CELL_SIZE + CELL_SIZE / 2
    ctx.fillStyle = attack.type === 'scout' ? '#9333ea' : '#dc2626'
    ctx.beginPath()
    ctx.arc(dotX, dotY, 4, 0, Math.PI * 2)
    ctx.fill()
  }

  for (const city of mapCities) {
    const tier = citySpriteIndex(city.id)
    const citySprite = sprites.cities[tier]
    if (!citySprite) continue

    ctx.drawImage(
      citySprite,
      city.x * CELL_SIZE,
      city.y * CELL_SIZE,
      CELL_SIZE,
      CELL_SIZE,
    )
  }

  if (selectedTile) {
    ctx.fillStyle = 'rgba(59, 130, 246, 0.35)'
    ctx.fillRect(
      selectedTile.x * CELL_SIZE,
      selectedTile.y * CELL_SIZE,
      CELL_SIZE,
      CELL_SIZE,
    )
  }
}

function MapCanvas({ mapSize, worldSeed }) {
  const canvasRef = useRef(null)
  const terrainRef = useRef(null)
  const { selection, setSelection } = useGameUI()
  const { mapCities } = useGameData()
  const { currentTick } = useWorld()
  const [sprites, setSprites] = useState(null)
  const [mapReady, setMapReady] = useState(false)
  const [attacks, setAttacks] = useState([])

  const selectedTileId = selection?.type === 'tile' ? selection.id : null

  useEffect(() => {
    let cancelled = false

    loadMapSprites()
      .then((loaded) => {
        if (!cancelled) {
          setSprites(loaded)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setSprites(null)
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!sprites || !worldSeed || !mapSize) {
      terrainRef.current = null
      setMapReady(false)
      return
    }

    terrainRef.current = buildTerrainLayer(sprites, worldSeed, mapSize, CELL_SIZE)
    setMapReady(true)
  }, [sprites, worldSeed, mapSize])

  useEffect(() => {
    let cancelled = false

    api
      .getAttacks()
      .then((data) => {
        if (!cancelled) setAttacks(data)
      })
      .catch(() => {
        if (!cancelled) setAttacks([])
      })

    return () => {
      cancelled = true
    }
  }, [currentTick])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas || !sprites || !mapReady || !terrainRef.current) return

    const selectedTile = selectedTileId ? parseTileId(selectedTileId) : null

    drawMap({
      canvas,
      sprites,
      terrainLayer: terrainRef.current,
      mapSize,
      selectedTile,
      mapCities,
      attacks,
    })
  }, [selectedTileId, mapCities, sprites, mapReady, mapSize, attacks])

  const handleClick = useCallback(
    (event) => {
      const canvas = canvasRef.current
      if (!canvas) return

      const rect = canvas.getBoundingClientRect()
      const x = Math.floor(
        ((event.clientX - rect.left) / rect.width) * mapSize,
      )
      const y = Math.floor(
        ((event.clientY - rect.top) / rect.height) * mapSize,
      )

      if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return

      setSelection({ type: 'tile', id: tileId(x, y) })
    },
    [mapSize, setSelection],
  )

  const mapWidth = mapSize * CELL_SIZE
  const mapHeight = mapSize * CELL_SIZE

  return (
    <div className="relative">
      {!mapReady && (
        <div className="absolute inset-0 z-10 flex items-center justify-center bg-muted/80 text-sm text-muted-foreground">
          Loading map…
        </div>
      )}
      <canvas
        ref={canvasRef}
        width={mapWidth}
        height={mapHeight}
        onClick={handleClick}
        className="cursor-crosshair touch-none"
        aria-label="World map"
      />
    </div>
  )
}

function MapControls({ containerRef, mapSize }) {
  const { setTransform } = useControls()
  const { primaryCity } = useGameData()

  const jumpToCity = useCallback(() => {
    if (!primaryCity) return

    const container = containerRef.current
    if (!container) return

    const scale = 1.5
    const { width, height } = container.getBoundingClientRect()
    const centerX = primaryCity.x * CELL_SIZE + CELL_SIZE / 2
    const centerY = primaryCity.y * CELL_SIZE + CELL_SIZE / 2

    setTransform(
      width / 2 - centerX * scale,
      height / 2 - centerY * scale,
      scale,
      200,
    )
  }, [containerRef, primaryCity, setTransform])

  return (
    <div className="absolute right-3 top-3 z-10">
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={jumpToCity}
        disabled={!primaryCity}
      >
        Jump to city
      </Button>
    </div>
  )
}

function MapGrid() {
  const containerRef = useRef(null)
  const { worldSeed, mapSize, loading: worldLoading } = useWorld()
  const effectiveMapSize = mapSize || 100
  const effectiveSeed = worldSeed || 1

  if (worldLoading && !worldSeed) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Loading map…
      </div>
    )
  }

  return (
    <div ref={containerRef} className="relative h-full min-h-0 overflow-hidden">
      <TransformWrapper
        initialScale={0.8}
        minScale={0.2}
        maxScale={4}
        limitToBounds={false}
        smooth={false}
        panning={{ velocityDisabled: true }}
        wheel={{ step: 0.06 }}
        zoomAnimation={{ animationTime: 200, animationType: 'easeOut' }}
      >
        <MapControls containerRef={containerRef} mapSize={effectiveMapSize} />
        <TransformComponent
          wrapperClass="!h-full !w-full"
          contentClass="!h-full !w-full"
        >
          <MapCanvas mapSize={effectiveMapSize} worldSeed={effectiveSeed} />
        </TransformComponent>
      </TransformWrapper>
    </div>
  )
}

export default MapGrid
