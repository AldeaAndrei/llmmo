import { useEffect, useRef, useState } from 'react'
import { cn } from '@/lib/utils'
import { buildingImageTier, getBuildingImageUrl } from '@/lib/buildingImages'

function BuildingImage({ type, level, name, size = 'sm', className }) {
  const tier = buildingImageTier(level)
  const src = getBuildingImageUrl(type, level)
  const prevTierRef = useRef(tier)
  const [upgraded, setUpgraded] = useState(false)

  useEffect(() => {
    if (tier > prevTierRef.current) {
      setUpgraded(true)
      const timer = window.setTimeout(() => setUpgraded(false), 700)
      prevTierRef.current = tier
      return () => window.clearTimeout(timer)
    }

    prevTierRef.current = tier
  }, [tier])

  const sizeClass =
    size === 'fill'
      ? 'max-h-full max-w-full'
      : size === 'lg'
        ? 'h-24 w-24'
        : size === 'md'
          ? 'h-14 w-14'
          : 'h-10 w-10'

  if (!src) {
    return (
      <div
        className={cn(
          'shrink-0 rounded-md border border-dashed border-border bg-muted/40',
          sizeClass,
          className,
        )}
        aria-hidden
      />
    )
  }

  return (
    <img
      src={src}
      alt={name ? `${name} (tier ${tier})` : ''}
      className={cn(
        'object-contain',
        size !== 'fill' && 'shrink-0 rounded-md',
        sizeClass,
        upgraded && 'animate-building-tier-up',
        className,
      )}
    />
  )
}

export default BuildingImage
