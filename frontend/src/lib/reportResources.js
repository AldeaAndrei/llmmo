function resourceAmount(bundle, key) {
  if (!bundle || typeof bundle !== 'object') {
    return 0
  }

  if (bundle[key] != null) {
    return bundle[key]
  }

  const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
  return bundle[pascalKey] ?? 0
}

export function formatResourceLine(resources) {
  return `Wood ${resourceAmount(resources, 'wood')} · Stone ${resourceAmount(resources, 'stone')} · Gold ${resourceAmount(resources, 'gold')} · Food ${resourceAmount(resources, 'food')}`
}
