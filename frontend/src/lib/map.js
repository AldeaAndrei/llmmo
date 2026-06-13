export const MAP_SIZE = 100
export const CELL_SIZE = 20
export const HOME_TILE = { x: 50, y: 50 }

export function tileId(x, y) {
  return `${x},${y}`
}

export function parseTileId(id) {
  const [x, y] = id.split(',').map(Number)
  return { x, y }
}

export function isHomeTile(x, y) {
  return x === HOME_TILE.x && y === HOME_TILE.y
}
