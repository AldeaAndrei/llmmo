import { createSeededNoise2D, terrainTypeAt } from '@/lib/terrainNoise'

const terrainLayerCache = new Map()

function cacheKey(worldSeed, mapSize, cellSize) {
  return `${worldSeed}:${mapSize}:${cellSize}`
}

export function buildTerrainLayer(sprites, worldSeed, mapSize, cellSize) {
  const key = cacheKey(worldSeed, mapSize, cellSize)
  const cached = terrainLayerCache.get(key)
  if (cached) {
    return cached
  }

  const canvas = document.createElement('canvas')
  canvas.width = mapSize * cellSize
  canvas.height = mapSize * cellSize

  const ctx = canvas.getContext('2d')
  if (!ctx) {
    return canvas
  }

  const noise2D = createSeededNoise2D(worldSeed)

  for (let y = 0; y < mapSize; y += 1) {
    for (let x = 0; x < mapSize; x += 1) {
      const type = terrainTypeAt(noise2D, x, y)
      const sprite = sprites.terrain[type]
      if (sprite) {
        ctx.drawImage(sprite, x * cellSize, y * cellSize, cellSize, cellSize)
      }
    }
  }

  terrainLayerCache.set(key, canvas)
  return canvas
}

const terrainGridCache = new Map()

/**
 * Per-tile terrain type lookup (mapSize x mapSize), so the viewport renderer can
 * redraw only the visible tiles from the hi-res source sprites when zoomed in,
 * instead of upscaling the prebaked overview layer. Cached per world/size.
 */
export function buildTerrainGrid(worldSeed, mapSize) {
  const key = `${worldSeed}:${mapSize}`
  const cached = terrainGridCache.get(key)
  if (cached) {
    return cached
  }

  const noise2D = createSeededNoise2D(worldSeed)
  const grid = new Array(mapSize)
  for (let y = 0; y < mapSize; y += 1) {
    const row = new Array(mapSize)
    for (let x = 0; x < mapSize; x += 1) {
      row[x] = terrainTypeAt(noise2D, x, y)
    }
    grid[y] = row
  }

  terrainGridCache.set(key, grid)
  return grid
}

export function clearTerrainCache() {
  terrainLayerCache.clear()
  terrainGridCache.clear()
}

export function hashString(value) {
  let hash = 2166136261

  for (let i = 0; i < value.length; i += 1) {
    hash ^= value.charCodeAt(i)
    hash = Math.imul(hash, 16777619)
  }

  return hash >>> 0
}

export function citySpriteIndex(cityId) {
  return (hashString(String(cityId)) % 5) + 1
}

export function drawGrid(ctx, mapSize, cellSize) {
  const width = mapSize * cellSize
  const height = mapSize * cellSize

  ctx.save()
  ctx.strokeStyle = 'rgba(0, 0, 0, 0.08)'
  ctx.lineWidth = 1

  for (let i = 0; i <= mapSize; i += 1) {
    const pos = i * cellSize
    ctx.beginPath()
    ctx.moveTo(pos, 0)
    ctx.lineTo(pos, height)
    ctx.stroke()
    ctx.beginPath()
    ctx.moveTo(0, pos)
    ctx.lineTo(width, pos)
    ctx.stroke()
  }

  ctx.restore()
}
