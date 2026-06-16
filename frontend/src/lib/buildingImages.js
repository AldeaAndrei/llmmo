const images = import.meta.glob('@/assets/buildings/*.png', {
  eager: true,
  import: 'default',
})

const imageByKey = new Map()

for (const [path, url] of Object.entries(images)) {
  const match = path.match(/\/([^/]+)_(\d)\.png$/)
  if (!match) {
    continue
  }

  imageByKey.set(`${match[1]}_${match[2]}`, url)
}

export function buildingImageTier(level) {
  const safeLevel = Math.max(1, level ?? 1)
  return Math.min(3, Math.max(1, Math.ceil(safeLevel / 5)))
}

export function getBuildingImageUrl(type, level) {
  if (!type) {
    return null
  }

  const tier = buildingImageTier(level)
  return imageByKey.get(`${type}_${tier}`) ?? null
}
