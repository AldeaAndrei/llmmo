import { useCallback, useEffect, useRef, useState } from 'react'
import { TransformWrapper, TransformComponent } from 'react-zoom-pan-pinch'
import { Button } from '@/components/ui/button'
import { useGameData } from '@/context/GameDataContext'
import { useGameUI } from '@/context/GameUIContext'
import { useWorld } from '@/context/WorldContext'
import {
  buildTerrainLayer,
  buildTerrainGrid,
  citySpriteIndex,
} from '@/lib/mapTerrain'
import { loadMapSprites } from '@/lib/mapSprites'
import { api } from '@/lib/api'
import { CELL_SIZE, parseTileId, tileId } from '@/lib/map'

const MIN_SCALE = 0.2
const MAX_SCALE = 8
// Constant fraction of zoom per wheel notch -> the zoom feels identical at every
// distance instead of "getting harder" the closer you are.
const ZOOM_SENSITIVITY = 0.0015
// Above this scale a source tile occupies more on-screen pixels than the prebaked
// overview layer holds, so we redraw the visible tiles from the hi-res sprites.
const SHARP_SCALE = 1

function centerOnTile(transform, viewW, viewH, tileX, tileY, scale, animationMs) {
  const cx = tileX * CELL_SIZE + CELL_SIZE / 2
  const cy = tileY * CELL_SIZE + CELL_SIZE / 2
  transform.setTransform(
    viewW / 2 - cx * scale,
    viewH / 2 - cy * scale,
    scale,
    animationMs,
  )
}

function MapGrid() {
  const containerRef = useRef(null)
  const transformRef = useRef(null)
  const canvasRef = useRef(null)

  const { worldSeed, mapSize: worldMapSize, loading: worldLoading, currentTick } = useWorld()
  const mapSize = worldMapSize || 100
  const seed = worldSeed || 1

  const { mapCities, primaryCity } = useGameData()
  const { selection, setSelection } = useGameUI()

  const [sprites, setSprites] = useState(null)
  const [mapReady, setMapReady] = useState(false)
  const [attacks, setAttacks] = useState([])

  const terrainLayerRef = useRef(null)
  const terrainGridRef = useRef(null)
  const cameraRef = useRef({ scale: 1, positionX: 0, positionY: 0 })
  const rafRef = useRef(0)
  const drawRef = useRef(() => {})
  const didCenterRef = useRef(false)

  const selectedTile =
    selection?.type === 'tile' ? parseTileId(selection.id) : null

  // --- load sprites once -------------------------------------------------
  useEffect(() => {
    let cancelled = false
    loadMapSprites()
      .then((loaded) => !cancelled && setSprites(loaded))
      .catch(() => !cancelled && setSprites(null))
    return () => {
      cancelled = true
    }
  }, [])

  // --- prebake the overview layer + terrain grid -------------------------
  useEffect(() => {
    if (!sprites) {
      terrainLayerRef.current = null
      terrainGridRef.current = null
      setMapReady(false)
      return
    }
    terrainLayerRef.current = buildTerrainLayer(sprites, seed, mapSize, CELL_SIZE)
    terrainGridRef.current = buildTerrainGrid(seed, mapSize)
    setMapReady(true)
  }, [sprites, seed, mapSize])

  // --- live troop movements ---------------------------------------------
  useEffect(() => {
    let cancelled = false
    api
      .getAttacks()
      .then((data) => !cancelled && setAttacks(data))
      .catch(() => !cancelled && setAttacks([]))
    return () => {
      cancelled = true
    }
  }, [currentTick])

  // --- the renderer ------------------------------------------------------
  // Draws only what the viewport covers, at device resolution, so close-ups
  // stay crisp while cost stays bounded by the screen rather than the map.
  const drawFrame = () => {
    const canvas = canvasRef.current
    const terrainLayer = terrainLayerRef.current
    const grid = terrainGridRef.current
    if (!canvas || !sprites || !terrainLayer || !grid) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const dpr = window.devicePixelRatio || 1
    const cssW = canvas.clientWidth
    const cssH = canvas.clientHeight
    if (!cssW || !cssH) return

    const backW = Math.round(cssW * dpr)
    const backH = Math.round(cssH * dpr)
    if (canvas.width !== backW || canvas.height !== backH) {
      canvas.width = backW
      canvas.height = backH
    }

    ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
    ctx.imageSmoothingEnabled = true
    ctx.imageSmoothingQuality = 'high'
    ctx.clearRect(0, 0, cssW, cssH)

    const { scale: s, positionX: ox, positionY: oy } = cameraRef.current
    const mapPx = mapSize * CELL_SIZE
    const tilePx = CELL_SIZE * s
    const worldToScreenX = (wx) => wx * s + ox
    const worldToScreenY = (wy) => wy * s + oy

    // visible map-pixel rectangle, clamped to the map bounds
    const vx0 = Math.max(0, -ox / s)
    const vy0 = Math.max(0, -oy / s)
    const vx1 = Math.min(mapPx, (cssW - ox) / s)
    const vy1 = Math.min(mapPx, (cssH - oy) / s)
    if (vx1 <= vx0 || vy1 <= vy0) return // map entirely off-screen

    // terrain: hi-res per-tile when zoomed in, single prebaked blit otherwise
    if (s > SHARP_SCALE) {
      const tx0 = Math.max(0, Math.floor(vx0 / CELL_SIZE))
      const ty0 = Math.max(0, Math.floor(vy0 / CELL_SIZE))
      const tx1 = Math.min(mapSize - 1, Math.floor((vx1 - 1e-3) / CELL_SIZE))
      const ty1 = Math.min(mapSize - 1, Math.floor((vy1 - 1e-3) / CELL_SIZE))
      for (let ty = ty0; ty <= ty1; ty += 1) {
        for (let tx = tx0; tx <= tx1; tx += 1) {
          const sprite = sprites.terrain[grid[ty][tx]]
          if (!sprite) continue
          // +1 dest size hides sub-pixel seams between neighbouring tiles
          ctx.drawImage(
            sprite,
            worldToScreenX(tx * CELL_SIZE),
            worldToScreenY(ty * CELL_SIZE),
            tilePx + 1,
            tilePx + 1,
          )
        }
      }
    } else {
      ctx.drawImage(
        terrainLayer,
        vx0,
        vy0,
        vx1 - vx0,
        vy1 - vy0,
        worldToScreenX(vx0),
        worldToScreenY(vy0),
        (vx1 - vx0) * s,
        (vy1 - vy0) * s,
      )
    }

    // grid lines only once tiles are large enough to read
    if (s > 1) {
      ctx.save()
      ctx.strokeStyle = 'rgba(0, 0, 0, 0.08)'
      ctx.lineWidth = 1
      const i0 = Math.max(0, Math.floor(vx0 / CELL_SIZE))
      const i1 = Math.min(mapSize, Math.ceil(vx1 / CELL_SIZE))
      for (let i = i0; i <= i1; i += 1) {
        const x = worldToScreenX(i * CELL_SIZE)
        ctx.beginPath()
        ctx.moveTo(x, worldToScreenY(vy0))
        ctx.lineTo(x, worldToScreenY(vy1))
        ctx.stroke()
      }
      const j0 = Math.max(0, Math.floor(vy0 / CELL_SIZE))
      const j1 = Math.min(mapSize, Math.ceil(vy1 / CELL_SIZE))
      for (let j = j0; j <= j1; j += 1) {
        const y = worldToScreenY(j * CELL_SIZE)
        ctx.beginPath()
        ctx.moveTo(worldToScreenX(vx0), y)
        ctx.lineTo(worldToScreenX(vx1), y)
        ctx.stroke()
      }
      ctx.restore()
    }

    // troop movements
    for (const attack of attacks ?? []) {
      const sx = worldToScreenX(attack.source.x * CELL_SIZE + CELL_SIZE / 2)
      const sy = worldToScreenY(attack.source.y * CELL_SIZE + CELL_SIZE / 2)
      const tx = worldToScreenX(attack.target.x * CELL_SIZE + CELL_SIZE / 2)
      const ty = worldToScreenY(attack.target.y * CELL_SIZE + CELL_SIZE / 2)
      ctx.strokeStyle =
        attack.type === 'scout' ? 'rgba(147, 51, 234, 0.5)' : 'rgba(220, 38, 38, 0.5)'
      ctx.lineWidth = 2
      ctx.beginPath()
      ctx.moveTo(sx, sy)
      ctx.lineTo(tx, ty)
      ctx.stroke()

      const dx = worldToScreenX(attack.currentX * CELL_SIZE + CELL_SIZE / 2)
      const dy = worldToScreenY(attack.currentY * CELL_SIZE + CELL_SIZE / 2)
      ctx.fillStyle = attack.type === 'scout' ? '#9333ea' : '#dc2626'
      ctx.beginPath()
      ctx.arc(dx, dy, 4, 0, Math.PI * 2)
      ctx.fill()
    }

    // cities: always drawn from the hi-res source at on-screen size
    for (const city of mapCities) {
      const dx = worldToScreenX(city.x * CELL_SIZE)
      const dy = worldToScreenY(city.y * CELL_SIZE)
      if (dx > cssW || dy > cssH || dx + tilePx < 0 || dy + tilePx < 0) continue
      const sprite = sprites.cities[citySpriteIndex(city.id)]
      if (sprite) ctx.drawImage(sprite, dx, dy, tilePx, tilePx)
    }

    // selection highlight
    if (selectedTile) {
      ctx.fillStyle = 'rgba(59, 130, 246, 0.35)'
      ctx.fillRect(
        worldToScreenX(selectedTile.x * CELL_SIZE),
        worldToScreenY(selectedTile.y * CELL_SIZE),
        tilePx,
        tilePx,
      )
    }
  }
  drawRef.current = drawFrame

  const scheduleRender = useCallback(() => {
    if (rafRef.current) cancelAnimationFrame(rafRef.current)
    rafRef.current = requestAnimationFrame(() => {
      rafRef.current = 0
      drawRef.current()
    })
  }, [])

  // redraw after every commit (data/selection changes) and on unmount cleanup
  useEffect(() => {
    scheduleRender()
    return () => {
      if (rafRef.current) cancelAnimationFrame(rafRef.current)
    }
  })

  // keep the canvas backing store in step with the container size
  useEffect(() => {
    const el = containerRef.current
    if (!el || typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(() => scheduleRender())
    observer.observe(el)
    return () => observer.disconnect()
  }, [scheduleRender])

  // cursor-anchored exponential zoom (replaces the library's additive wheel)
  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const onWheel = (event) => {
      event.preventDefault()
      const transform = transformRef.current
      if (!transform) return
      const { scale, positionX, positionY } = cameraRef.current
      const rect = el.getBoundingClientRect()
      const px = event.clientX - rect.left
      const py = event.clientY - rect.top
      const factor = Math.exp(-event.deltaY * ZOOM_SENSITIVITY)
      const next = Math.min(MAX_SCALE, Math.max(MIN_SCALE, scale * factor))
      if (next === scale) return
      const k = next / scale
      transform.setTransform(
        px - (px - positionX) * k,
        py - (py - positionY) * k,
        next,
        0,
      )
    }
    el.addEventListener('wheel', onWheel, { passive: false })
    return () => el.removeEventListener('wheel', onWheel)
  }, [])

  // center on the player's city on first load (map middle until it arrives)
  useEffect(() => {
    if (didCenterRef.current) return
    const el = containerRef.current
    const transform = transformRef.current
    if (!el || !transform) return
    const { width, height } = el.getBoundingClientRect()
    if (!width || !height) return

    if (primaryCity) {
      centerOnTile(transform, width, height, primaryCity.x, primaryCity.y, 1.8, 0)
      didCenterRef.current = true
    } else {
      const mid = Math.floor(mapSize / 2)
      centerOnTile(transform, width, height, mid, mid, 1, 0)
    }
  }, [primaryCity, mapReady, mapSize])

  const handleTransformed = useCallback((_ref, state) => {
    cameraRef.current = {
      scale: state.scale,
      positionX: state.positionX,
      positionY: state.positionY,
    }
    scheduleRender()
  }, [scheduleRender])

  const handleClick = useCallback(
    (event) => {
      const el = containerRef.current
      if (!el) return
      if (event.target.closest?.('button')) return // ignore overlay controls
      // Invert the exact camera mapping the renderer uses, so the picked tile
      // always matches what is drawn under the cursor.
      const rect = el.getBoundingClientRect()
      const { scale, positionX, positionY } = cameraRef.current
      const worldX = (event.clientX - rect.left - positionX) / scale
      const worldY = (event.clientY - rect.top - positionY) / scale
      const x = Math.floor(worldX / CELL_SIZE)
      const y = Math.floor(worldY / CELL_SIZE)
      if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return
      setSelection({ type: 'tile', id: tileId(x, y) })
    },
    [mapSize, setSelection],
  )

  const jumpToCity = useCallback(() => {
    const el = containerRef.current
    const transform = transformRef.current
    if (!el || !transform || !primaryCity) return
    const { width, height } = el.getBoundingClientRect()
    centerOnTile(transform, width, height, primaryCity.x, primaryCity.y, 1.8, 300)
  }, [primaryCity])

  if (worldLoading && !worldSeed) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Loading map…
      </div>
    )
  }

  const mapPx = mapSize * CELL_SIZE

  return (
    <div
      ref={containerRef}
      onClick={handleClick}
      className="relative h-full min-h-0 cursor-crosshair overflow-hidden"
    >
      <canvas
        ref={canvasRef}
        className="pointer-events-none absolute inset-0 h-full w-full"
        aria-label="World map"
      />

      {!mapReady && (
        <div className="absolute inset-0 z-10 flex items-center justify-center bg-muted/80 text-sm text-muted-foreground">
          Loading map…
        </div>
      )}

      <div className="absolute right-3 top-3 z-20">
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

      <TransformWrapper
        ref={transformRef}
        initialScale={1}
        minScale={MIN_SCALE}
        maxScale={MAX_SCALE}
        limitToBounds={false}
        smooth={false}
        wheel={{ disabled: true }}
        doubleClick={{ disabled: true }}
        panning={{ velocityDisabled: true }}
        onTransform={handleTransformed}
      >
        <TransformComponent wrapperClass="!h-full !w-full" contentClass="!h-full !w-full">
          {/* transparent surface rzpp pans/zooms; clicks are handled on the
              container so the whole viewport stays pickable at any pan/zoom */}
          <div style={{ width: mapPx, height: mapPx }} className="touch-none" />
        </TransformComponent>
      </TransformWrapper>
    </div>
  )
}

export default MapGrid
