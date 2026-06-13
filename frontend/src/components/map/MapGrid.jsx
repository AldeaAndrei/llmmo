import { useCallback, useEffect, useRef } from 'react'
import { TransformWrapper, TransformComponent, useControls } from 'react-zoom-pan-pinch'
import { Button } from '@/components/ui/button'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'
import {
  CELL_SIZE,
  MAP_SIZE,
  parseTileId,
  tileId,
} from '@/lib/map'

const MAP_WIDTH = MAP_SIZE * CELL_SIZE
const MAP_HEIGHT = MAP_SIZE * CELL_SIZE

function drawMap(canvas, selectedTile, mapCities, playerId) {
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  ctx.clearRect(0, 0, MAP_WIDTH, MAP_HEIGHT)

  ctx.fillStyle = '#fafafa'
  ctx.fillRect(0, 0, MAP_WIDTH, MAP_HEIGHT)

  ctx.strokeStyle = '#e4e4e7'
  ctx.lineWidth = 1

  for (let i = 0; i <= MAP_SIZE; i++) {
    const pos = i * CELL_SIZE
    ctx.beginPath()
    ctx.moveTo(pos, 0)
    ctx.lineTo(pos, MAP_HEIGHT)
    ctx.stroke()
    ctx.beginPath()
    ctx.moveTo(0, pos)
    ctx.lineTo(MAP_WIDTH, pos)
    ctx.stroke()
  }

  for (const city of mapCities) {
    const isOwn = playerId && city.playerId === playerId
    ctx.fillStyle = isOwn ? 'rgba(34, 197, 94, 0.75)' : 'rgba(249, 115, 22, 0.75)'
    ctx.fillRect(
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

function MapCanvas() {
  const canvasRef = useRef(null)
  const { selection, setSelection } = useGameUI()
  const { mapCities, playerId } = useGameData()

  const selectedTileId = selection?.type === 'tile' ? selection.id : null

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const selectedTile = selectedTileId ? parseTileId(selectedTileId) : null
    drawMap(canvas, selectedTile, mapCities, playerId)
  }, [selectedTileId, mapCities, playerId])

  const handleClick = useCallback(
    (event) => {
      const canvas = canvasRef.current
      if (!canvas) return

      const rect = canvas.getBoundingClientRect()
      const x = Math.floor(
        ((event.clientX - rect.left) / rect.width) * MAP_SIZE,
      )
      const y = Math.floor(
        ((event.clientY - rect.top) / rect.height) * MAP_SIZE,
      )

      if (x < 0 || x >= MAP_SIZE || y < 0 || y >= MAP_SIZE) return

      setSelection({ type: 'tile', id: tileId(x, y) })
    },
    [setSelection],
  )

  return (
    <canvas
      ref={canvasRef}
      width={MAP_WIDTH}
      height={MAP_HEIGHT}
      onClick={handleClick}
      className="cursor-crosshair touch-none"
      aria-label="World map"
    />
  )
}

function MapControls({ containerRef }) {
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
        <MapControls containerRef={containerRef} />
        <TransformComponent
          wrapperClass="!h-full !w-full"
          contentClass="!h-full !w-full"
        >
          <MapCanvas />
        </TransformComponent>
      </TransformWrapper>
    </div>
  )
}

export default MapGrid
