export const MAP_SIZE = 100
export const CELL_SIZE = 20

export function tileId(x, y) {
  return `${x},${y}`
}

export function parseTileId(id) {
  const [x, y] = id.split(',').map(Number)
  return { x, y }
}

export function findCityAt(cities, x, y) {
  return cities.find((city) => city.x === x && city.y === y) ?? null
}

export function findFreeTile(mapCities) {
  const occupied = new Set(mapCities.map((city) => tileId(city.x, city.y)))

  for (let attempt = 0; attempt < 500; attempt++) {
    const x = Math.floor(Math.random() * MAP_SIZE)
    const y = Math.floor(Math.random() * MAP_SIZE)
    if (!occupied.has(tileId(x, y))) {
      return { x, y }
    }
  }

  throw new Error('No free tiles on the map')
}
