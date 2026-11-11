const clamp = (n, min, max) => Math.max(min, Math.min(max, n))

// The camera follows the controlled skater, puck, and defensive partner.
// The overview always displays the full rink and this exact viewport rectangle.
export function cameraFor(game, viewport) {
  const { width: screenWidth, height: screenHeight } = viewport
  if (screenWidth > 1000 || (screenWidth > 900 && screenHeight > 520))
    return { x: 0, y: 70, width: 1260, height: 650 }

  const portrait = screenWidth <= 680 && screenHeight >= screenWidth
  const shortLandscape = screenWidth <= 1000 && screenHeight <= 520 && screenWidth > screenHeight
  const base = portrait ? { width: 520, height: 650 }
    : shortLandscape ? { width: 700, height: 210 }
      : { width: 700, height: 540 }
  const points = [game.players[game.selected], game.players[4], game.puck, { x: 99, y: 397 }]
  const minX = Math.min(...points.map(p => p.x)), maxX = Math.max(...points.map(p => p.x))
  const minY = Math.min(...points.map(p => p.y)), maxY = Math.max(...points.map(p => p.y))
  const width = clamp(Math.max(base.width, maxX - minX + 100), base.width, 1260)
  const height = clamp(Math.max(base.height, maxY - minY + 90), base.height, 650)
  return {
    x: clamp((minX + maxX - width) / 2, 0, 1260 - width),
    y: clamp((minY + maxY - height) / 2, 70, 720 - height),
    width, height,
  }
}

export const cameraViewBox = camera => `${camera.x} ${camera.y} ${camera.width} ${camera.height}`
