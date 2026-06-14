function fade(t) {
  return t * t * t * (t * (t * 6 - 15) + 10)
}

function lerp(a, b, t) {
  return a + t * (b - a)
}

function grad(hash, x, y) {
  const h = hash & 3
  const u = h < 2 ? x : y
  const v = h < 2 ? y : x
  return (h & 1 ? -u : u) + (h & 2 ? -v : v)
}

export function createSeededNoise2D(seed) {
  const permutation = new Uint8Array(256)
  for (let i = 0; i < 256; i += 1) {
    permutation[i] = i
  }

  let state = seed >>> 0
  for (let i = 255; i > 0; i -= 1) {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0
    const j = state % (i + 1)
    const tmp = permutation[i]
    permutation[i] = permutation[j]
    permutation[j] = tmp
  }

  const table = new Uint8Array(512)
  for (let i = 0; i < 512; i += 1) {
    table[i] = permutation[i & 255]
  }

  return function noise2D(x, y) {
    const xi = Math.floor(x) & 255
    const yi = Math.floor(y) & 255
    const xf = x - Math.floor(x)
    const yf = y - Math.floor(y)
    const u = fade(xf)
    const v = fade(yf)

    const aa = table[table[xi] + yi]
    const ab = table[table[xi] + yi + 1]
    const ba = table[table[xi + 1] + yi]
    const bb = table[table[xi + 1] + yi + 1]

    return lerp(
      lerp(grad(aa, xf, yf), grad(ba, xf - 1, yf), u),
      lerp(grad(ab, xf, yf - 1), grad(bb, xf - 1, yf - 1), u),
      v,
    )
  }
}

export function terrainTypeAt(noise2D, x, y) {
  const value = (noise2D(x * 0.12, y * 0.12) + 1) / 2

  if (value > 0.92) {
    return 'rock'
  }

  if (value > 0.85) {
    return 'tree'
  }

  return 'grass'
}
