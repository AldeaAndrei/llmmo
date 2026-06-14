import grassUrl from '@/assets/map/grass.png'
import rockUrl from '@/assets/map/rock.png'
import treeUrl from '@/assets/map/tree.png'
import city1Url from '@/assets/map/city-1.png'
import city2Url from '@/assets/map/city-2.png'
import city3Url from '@/assets/map/city-3.png'
import city4Url from '@/assets/map/city-4.png'
import city5Url from '@/assets/map/city-5.png'

function loadImage(url) {
  return new Promise((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error(`Failed to load map sprite: ${url}`))
    image.src = url
  })
}

let spritesPromise = null

export function loadMapSprites() {
  if (!spritesPromise) {
    spritesPromise = Promise.all([
      loadImage(grassUrl),
      loadImage(rockUrl),
      loadImage(treeUrl),
      loadImage(city1Url),
      loadImage(city2Url),
      loadImage(city3Url),
      loadImage(city4Url),
      loadImage(city5Url),
    ]).then(([grass, rock, tree, city1, city2, city3, city4, city5]) => ({
      terrain: { grass, rock, tree },
      cities: { 1: city1, 2: city2, 3: city3, 4: city4, 5: city5 },
    }))
  }

  return spritesPromise
}
