import test from 'node:test'
import assert from 'node:assert/strict'
import { cameraFor } from './camera.js'
import { act, chooseIntent, newGame, setTarget, startGame, steerWithKeys, stopAtPlayer, tick, togglePause } from './game.js'

const runUntil = (game, predicate, limit = 400) => {
  let current = game
  for (let i = 0; i < limit && !predicate(current); i++) current = tick(current)
  assert.ok(predicate(current), 'expected game state was reached')
  return current
}
const upperPointState = (weakWingCovered) => {
  const game = startGame(newGame('free'))
  return { ...game, elapsed: 17.9, seconds: 12.1, partnerSet: true, passMade: true,
    puck: { x: 333, y: 244, owner: 8, flight: null },
    players: game.players.map(p => p.id === 8 ? { ...p, x: 350, y: 235 }
      : p.id === 2 ? { ...p, x: 320, y: 245 }
        : p.id === 1 ? { ...p, x: weakWingCovered ? 280 : 400, y: weakWingCovered ? 410 : 580 } : p) }
}

test('opponent seeds reproduce distinct plays and record the pass in replay', () => {
  const play = seed => runUntil(startGame(newGame('free', seed)), g => g.passMade)
  const cycle = play(0)
  const feed = play(1)
  assert.equal(cycle.puck.flight.receiver, 10)
  assert.equal(feed.puck.flight.receiver, 9)
  assert.ok(feed.cornerExitSeen && feed.partnerSet)
  assert.equal(feed.history.at(-1).event, 'Red feeds the slot')
  assert.deepEqual(feed.history, play(1).history)

  const paused = runUntil(startGame(newGame('guided', 1)), g => g.pause === 'teaching')
  const guided = runUntil(chooseIntent(paused, 'hold'), g => g.passMade)
  assert.equal(guided.puck.flight.receiver, 9)
  assert.equal(guided.history.at(-1).event, feed.history.at(-1).event)
})

test('a seeded net-front feed gives open Red C a shot and a tie-up can deny it', () => {
  const opening = runUntil(startGame(newGame('free', 1)), g => g.puck.flight?.receiver === 9)
  const center = opening.players[9]
  const reposition = (x, y) => setTarget({ ...opening,
    players: opening.players.map(p => p.id === 2 ? { ...p, x, y } : p),
  }, x, y)
  const open = runUntil(reposition(300, 360), g => g.event === 'Red shot')
  assert.match(open.message, /Net-front shot/)
  const tied = act(reposition(center.x - 32, center.y - 30), 'tie')
  const denied = runUntil(tied, g => g.event === 'Pass denied')
  assert.equal(denied.history.at(-1).event, 'Pass denied')
  assert.equal(denied.puck.owner, null)
  const nearby = { ...denied, players: denied.players.map(p => p.id === denied.selected
    ? { ...p, x: denied.puck.x - 20, y: denied.puck.y } : p) }
  assert.equal(act(nearby, 'poke').puck.owner, nearby.selected)
})

test('the slot warning gives LD time to skate into reach and deny the pass', () => {
  let game = startGame(setTarget(newGame('free', 1), 300, 360))
  game = runUntil(game, g => g.slotPassAt != null)
  const warning = game
  assert.equal(warning.puck.owner, 6)
  assert.ok(warning.slotPassAt - warning.elapsed >= 1.49)
  assert.equal(act(warning, 'tie').event, 'Tie-up out of reach')

  game = runUntil(warning, g => g.elapsed >= warning.elapsed + 0.6)
  game = setTarget(game, 232, 365)
  game = runUntil(game, g => g.elapsed >= warning.elapsed + 1.4)
  game = act(game, 'tie')
  assert.equal(game.tieUp?.opponent, 9)
  game = runUntil(game, g => g.event === 'Pass denied')
  assert.equal(game.slotPassAt, null)
  assert.equal(warning.history.at(-1).slotPassAt, warning.slotPassAt)
})

test('live roster has five skaters and one goalie on each side', () => {
  const game = newGame()
  assert.equal(game.players.length, 12)
  for (const team of [0, 1]) {
    assert.equal(game.players.filter(p => p.team === team && p.slot !== 5).length, 5)
    assert.equal(game.players.filter(p => p.team === team && p.slot === 5).length, 1)
  }
  assert.equal(game.players[game.selected].role, 'LD')
  assert.equal(game.players[4].role, 'RD')
  assert.deepEqual([6,7,8,10].map(id => game.players[id].role), ['RW','LW','RD','LD'])
  assert.ok(game.players[7].x < 250 && game.players[7].y < 530, 'red LW starts closer to net and middle')
  assert.ok(game.players[10].y < 540, 'red LD starts closer to the middle lane')
  assert.equal(game.puck.owner, 6)
  assert.ok(Math.hypot(game.players[0].x - 242, game.players[0].y - 244) < 90, 'blue LW starts inside its circle')
  assert.ok(Math.hypot(game.players[1].x - 242, game.players[1].y - 550) < 90, 'blue RW starts inside its circle')
  assert.ok(game.players[0].y > 165 && game.players[1].y < 615, 'both wings start toward the middle lane')
  assert.ok(Math.abs(game.players[4].x - 99) < 40 && game.players[4].y > 550, 'blue RD starts by his goal-line corner')
  assert.ok(Math.hypot(game.players[6].x - 242, game.players[6].y - 244) < 90 && game.players[6].y > 300,
    'red carrier starts low on the LD-side circle')
  assert.ok(game.players[6].x < game.players[8].x, 'red carrier is below the point defender')
  assert.ok(game.players[9].x < game.players[6].x, 'red center starts near net front')
  assert.ok(Math.hypot(game.puck.x - game.players[6].x, game.puck.y - game.players[6].y) < 25,
    'puck starts with the low red carrier')
})

test('guided teaching pause freezes play, accepts intent, and resumes once', () => {
  let game = runUntil(startGame(newGame()), g => g.pause === 'teaching')
  assert.equal(game.elapsed, 2.5)
  assert.equal(game.running, false)
  assert.equal(tick(game), game)
  assert.equal(chooseIntent(game, 'invalid'), game)
  game = chooseIntent(game, 'hold')
  assert.equal(game.running, true)
  assert.equal(game.intent, 'hold')
  game = runUntil(game, g => g.elapsed >= 4)
  assert.equal(game.pause, null)
  assert.equal(game.taught, true)
})

test('freer practice has no automatic pause and records two distinct reads', () => {
  let game = startGame(newGame('free'))
  game = runUntil(game, g => g.decisions.length === 1)
  assert.equal(game.pause, null)
  assert.equal(game.decisions[0].result, 'met')
  game = runUntil(game, g => g.decisions.length === 2)
  assert.deepEqual(game.decisions.map(d => d.number), [1, 2])
  assert.deepEqual(game.decisions.map(d => d.result), ['met', 'met'])
  assert.equal(game.players[2].x, 185, 'LD does not have to chase the carrier')
})

test('early chase leaves net front but LD can recover Red C coverage', () => {
  let game = startGame(newGame('free'))
  game = setTarget(game, 280, 450)
  game = runUntil(game, g => g.cornerExitSeen)
  game = setTarget(game, 185, 360)
  game = runUntil(game, g => g.decisions.length === 2)
  assert.equal(game.decisions[0].result, 'missed')
  assert.equal(game.decisions[1].result, 'met')
})

test('manual pause freezes clock and puck; controls resume the same shift', () => {
  let game = tick(startGame(newGame('free')))
  game = togglePause(game)
  assert.equal(game.pause, 'manual')
  const frozen = tick(game)
  assert.equal(frozen, game)
  game = togglePause(game)
  assert.equal(game.running, true)
  assert.ok(tick(game).elapsed > frozen.elapsed)
})

test('RD recovers from the goal line while the low carrier advances and skaters hold distinct lanes', () => {
  const start = newGame('free')
  let game = runUntil(startGame(start), g => g.partnerSet)
  assert.ok(game.elapsed > 5 && game.elapsed < 9)
  assert.ok(game.players[4].y < start.players[4].y - 120)
  assert.ok(game.players[6].x < start.players[6].x - 10)
  assert.ok(game.players[9].y > game.players[6].y)
  assert.ok(game.players[8].x > game.players[6].x)
  assert.ok(game.players[0].y < game.players[1].y)
})

test('Box + 1 keeps the puck-side wing high, far wing in the slot, and center low', () => {
  const start = newGame('free')
  const game = runUntil(startGame(start), g => g.elapsed >= 9)
  assert.ok(game.players[0].x > game.players[6].x + 50, 'strong-side LW covers above the carrier')
  assert.ok(game.players[0].y >= 215 && game.players[0].y <= 255, 'strong-side LW stays near the point lane')
  assert.ok(game.players[1].y >= 375 && game.players[1].y <= 435, 'weak-side RW tucks into the high slot')
  assert.ok(game.puck.x < 99, 'red carrier has taken the puck behind the goal line')
  assert.ok(game.players[3].x >= 185 && game.players[3].x <= 225 && Math.abs(game.players[3].y - 397) < 25,
    'center protects the slot while the puck is behind the net')
  assert.ok(game.players[4].x >= 145 && game.players[4].x <= 170 &&
    game.players[4].y >= 440 && game.players[4].y <= 465,
  'RD guards the near post from outside the crease')
  assert.ok(Math.hypot(game.players[4].x - game.players[5].x,
    game.players[4].y - game.players[5].y) >= 55, 'RD stays clear of the goalie')
  assert.ok(game.players[3].x > game.players[4].x, 'center and RD keep distinct low lanes')
})

test('Box + 1 wings swap point and slot responsibilities when the puck crosses the zone', () => {
  let game = runUntil(startGame(newGame('free')), g => g.elapsed >= 9)
  game = { ...game, puck: { x: 230, y: 500, owner: 7, flight: null } }
  game = runUntil(game, g => g.elapsed >= 15)
  assert.ok(game.players[1].y >= 535 && game.players[1].x >= 285, 'RW takes the lower point lane')
  assert.ok(game.players[0].y >= 365 && game.players[0].y <= 420, 'LW rotates into the high slot')
  assert.ok(game.players[3].y < game.players[7].y, 'center rotates to lower-side support')
})

test('RD takes the strong-side inside lane when the puck changes sides', () => {
  let game = runUntil(startGame(newGame('free')), g => g.partnerSet)
  game = { ...game, puck: { x: 250, y: 530, owner: 7, flight: null } }
  game = runUntil(game, g => g.elapsed >= 10)
  assert.ok(game.players[9].y < game.players[7].y, 'net-front center stays inside the lower-side carrier')
  assert.ok(game.players[4].x < game.players[7].x, 'RD stays between carrier and net')
  assert.ok(game.players[4].y > game.players[9].y + 35, 'RD and red center occupy distinct lanes')
  assert.ok(Math.hypot(game.players[4].x - game.players[5].x,
    game.players[4].y - game.players[5].y) >= 65, 'RD comes away from the goalie')
})

test('RD contains the emerging lower-side carrier without crowding the goalie', () => {
  let game = startGame(newGame('free'))
  for (let i = 0; i < 180; i++) {
    game = tick(game)
    if (game.partnerSet && game.elapsed <= 18)
      assert.ok(Math.hypot(game.players[4].x - game.players[5].x,
        game.players[4].y - game.players[5].y) >= 55,
      `RD crowded goalie at ${game.elapsed}`)
    if (game.elapsed === 14) {
      assert.ok(game.players[4].x > 145, 'RD comes out of the crease area')
      assert.ok(Math.hypot(game.players[4].x - game.players[6].x,
        game.players[4].y - game.players[6].y) < 60, 'RD contains the carrier')
      assert.ok(Math.hypot(game.players[3].x - game.players[7].x,
        game.players[3].y - game.players[7].y) < 50, 'blue C tracks red LW')
    }
  }
})

test('a blue turnover sends teammates into outlets and opponents into a backcheck', () => {
  let game = startGame(newGame('free'))
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 230, y: 315 } : p) }
  game = act(game, 'poke')
  const before = game.players.map(p => ({ x: p.x, y: p.y }))
  game = runUntil(game, g => g.elapsed >= 4)
  assert.ok(game.players[0].x > before[0].x)
  assert.ok(game.players[8].x > before[8].x)
  assert.ok(Math.hypot(game.players[9].x - game.players[2].x, game.players[9].y - game.players[2].y) <
    Math.hypot(before[9].x - before[2].x, before[9].y - before[2].y))
})

test('red exits behind the net and cycles to the new puck-side point without teleporting', () => {
  let game = runUntil(startGame(newGame('free')), g => g.puck.flight?.kind === 'pass')
  assert.equal(game.cornerExitSeen, true)
  assert.equal(game.puck.flight.receiver, 10)
  const before = { ...game.puck }
  game = tick(game)
  assert.ok(Math.hypot(game.puck.x - before.x, game.puck.y - before.y) <= 15.01)
  game = runUntil(game, g => g.puck.owner === 10)
  assert.equal(game.event, 'Pass received')
})

test('pressure at the point finds the weak-side wing when the middle lane is open', () => {
  let game = upperPointState(false)
  game = tick(game)
  assert.equal(game.puck.flight?.kind, 'pass')
  assert.equal(game.puck.flight.receiver, 7)
  assert.equal(game.event, 'Red changes sides')
})

test('the point shot is saved while a far-side weak-side shot can score', () => {
  let point = runUntil(startGame(newGame('free')), g => g.event === 'Save')
  assert.deepEqual(point.score, [0, 0])
  let weak = startGame(newGame('free'))
  weak = { ...weak, elapsed: 21.9, seconds: 8.1, partnerSet: true,
    puck: { x: 250, y: 530, owner: 7, flight: null },
    players: weak.players.map(p => p.id === 5 ? { ...p, y: 429 } : p) }
  weak = runUntil(weak, g => g.event === 'Goal')
  assert.deepEqual(weak.score, [0, 1])
  point = runUntil(point, g => g.pause === 'ended')
  weak = runUntil(weak, g => g.pause === 'ended')
  assert.equal(point.outcome, 'Goalie covered the puck')
  assert.equal(weak.outcome, 'Red scores.')
})

test('pressure at the point routes around the covered middle lane', () => {
  let game = upperPointState(true)
  game = runUntil(game, g => g.event === 'Red switches points')
  assert.equal(game.puck.flight.receiver, 10)
  game = runUntil(game, g => g.puck.owner === 10)
  assert.equal(game.players[10].role, 'LD')
  game = runUntil(game, g => g.event === 'Save')
  assert.deepEqual(game.score, [0, 0])
})

test('winning the puck after chasing does not count as Red C coverage', () => {
  let game = runUntil(startGame(newGame('free')), g => g.partnerSet)
  game = runUntil(game, g => g.cornerExitSeen)
  game = setTarget(game, 205, 450)
  game = runUntil(game, g => g.elapsed >= 14.6)
  game = act(game, 'poke')
  assert.equal(game.puck.owner, 2)
  assert.equal(game.event, 'Turnover')
  assert.deepEqual(game.decisions.map(d => d.result), ['met', 'missed'])
})

test('a player clear exits the defending zone without counting as a goal', () => {
  let game = startGame(newGame('free'))
  game = { ...game, puck: { x: 185, y: 360, owner: 2, flight: null } }
  game = act(game, 'shoot')
  assert.equal(game.puck.flight.kind, 'clear')
  game = runUntil(game, g => g.event === 'Zone exit')
  assert.deepEqual(game.score, [0, 0])
  game = runUntil(game, g => g.pause === 'ended')
  assert.ok(game.elapsed < 30)
  assert.equal(game.outcome, 'Blue cleared the zone')
})

test('tie up needs Red C within stick reach and gives visible timed coverage', () => {
  let game = startGame(newGame('free'))
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 320, y: 360 } : p) }
  game = act(game, 'tie')
  assert.equal(game.tieUp, null)
  assert.equal(game.event, 'Tie-up out of reach')
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 185, y: 360 } : p) }
  game = act(game, 'tie')
  assert.equal(game.tieUp.opponent, 9)
  assert.equal(game.tieUps.length, 1)
  game = runUntil(game, g => g.elapsed >= 1)
  assert.equal(game.tieUp?.opponent, 9)
  assert.equal(game.history.at(-1).tieUp?.opponent, 9, 'tie-up appears in replay')
  game = runUntil(game, g => g.elapsed >= 2.6)
  assert.equal(game.tieUp, null, 'tie-up expires')
  game = act(game, 'tie')
  assert.equal(game.event, 'Tie-up recovering')
})

test('skating away breaks the tie-up and it denies a pass to Red C', () => {
  let game = act(startGame(newGame('free')), 'tie')
  game = { ...game, puck: { x: 220, y: 410, owner: null, flight: { kind: 'pass', receiver: 9 } } }
  game = tick(game)
  assert.equal(game.event, 'Pass denied')
  assert.equal(game.puck.owner, null)
  assert.equal(game.puck.flight, null)
  game = setTarget(game, 400, 360)
  game = runUntil(game, g => g.tieUp === null)
  assert.ok(game.elapsed < 2.5, 'moving out of reach ends the tie before its timer')
})

test('puck can be won, passed, and the shift ends with a restart path', () => {
  let game = startGame(newGame('free'))
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 230, y: 315 } : p) }
  game = act(game, 'poke')
  assert.equal(game.puck.owner, 2)
  game = act(game, 'pass')
  assert.equal(game.puck.owner, null)
  assert.equal(game.puck.flight.kind, 'pass')
  game = runUntil(game, g => g.pause === 'ended')
  assert.equal(game.running, false)
  assert.equal(game.decisions.length, 2)
  assert.equal(startGame(game), game)
  assert.equal(startGame(newGame(game.mode)).running, true)
})

test('releasing keyboard movement stops at the skater without changing tap-to-move', () => {
  let game = startGame(newGame('free'))
  game = setTarget(game, 500, 397)
  game = runUntil(game, g => g.elapsed >= 1)
  assert.ok(game.target.x > game.players[2].x)
  const stopped = stopAtPlayer(game)
  assert.equal(stopped.target.x, stopped.players[2].x)
  assert.equal(tick(stopped).players[2].x, stopped.players[2].x)
  const tapped = setTarget(stopped, 400, 400)
  assert.deepEqual(tapped.target, { x: 400, y: 400 })
  assert.ok(tick(tapped).players[2].x > stopped.players[2].x)
})

test('reversing Right to Left changes skating direction on the next tick', () => {
  let game = startGame(newGame('free'))
  const held = new Set(['ArrowRight'])
  for (let i = 0; i < 20; i++) game = tick(steerWithKeys(game, held))
  const before = game.players[2].x
  held.delete('ArrowRight')
  held.add('ArrowLeft')
  game = tick(steerWithKeys(game, held))
  assert.ok(game.players[2].x < before)
  assert.ok(game.target.x < before)
})

test('releasing Up while holding Right removes upward motion immediately', () => {
  let game = startGame(newGame('free'))
  const held = new Set(['ArrowUp', 'ArrowRight'])
  for (let i = 0; i < 12; i++) game = tick(steerWithKeys(game, held))
  const before = game.players[2]
  held.delete('ArrowUp')
  game = tick(steerWithKeys(game, held))
  assert.ok(game.players[2].x > before.x)
  assert.equal(game.players[2].y, before.y)
  assert.equal(game.target.y, before.y)
})

test('first read uses coverage through partner-set, not the final frame', () => {
  let game = setTarget(startGame(newGame('free')), 360, 335)
  game = runUntil(game, g => g.elapsed >= 6.2)
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 195, y: 397 } : p) }
  game = setTarget(game, 195, 397)
  game = runUntil(game, g => g.decisions.length === 1)
  assert.equal(game.partnerSet, true)
  assert.equal(game.decisions[0].result, 'missed')
  assert.ok(game.coverage.first.eligible > 30)
})

test('LD stays goal side of Red C when the lower-side red wing carries', () => {
  let game = runUntil(startGame(newGame('free')), g => g.partnerSet)
  game = { ...game, puck: { ...game.puck, owner: 7, flight: null } }
  game = setTarget(game, 185, 360)
  game = runUntil(game, g => g.decisions.length === 2)
  assert.equal(game.decisions[1].result, 'met')
  assert.ok(Math.hypot(game.players[2].x - game.players[7].x, game.players[2].y - game.players[7].y) > 65)
})

test('LD tracks Red C near the net against an upper-side carrier', () => {
  let game = runUntil(startGame(newGame('free')), g => g.partnerSet)
  game = { ...game, puck: { ...game.puck, owner: 8, flight: null } }
  game = setTarget(game, 185, 430)
  game = runUntil(game, g => g.decisions.length === 2)
  assert.equal(game.decisions[1].result, 'met')
})

test('guided feedback points to Red C when LD skates high', () => {
  let game = runUntil(startGame(newGame()), g => g.pause === 'teaching')
  game = chooseIntent(game, 'hold')
  game = runUntil(game, g => g.partnerSet)
  game = setTarget(game, 320, 340)
  game = runUntil(game, g => g.decisions.length === 2)
  assert.equal(game.decisions[1].result, 'missed')
  assert.equal(game.feedback.title, 'Red C got free')
  assert.ok(Math.hypot(game.feedback.focus.x - game.players[9].x,
    game.feedback.focus.y - game.players[9].y) < 5, 'highlight follows Red C')
  assert.ok(game.feedback.focus.x < 220, 'highlight is near the net, not the top of the zone')
})

test('red attackers keep connected support and point spacing through the wrap', () => {
  let game = startGame(newGame('free'))
  for (let i = 0; i < 200; i++) {
    game = tick(game)
    for (let a = 6; a <= 10; a++) for (let b = a + 1; b <= 10; b++)
      assert.ok(Math.hypot(game.players[a].x - game.players[b].x,
        game.players[a].y - game.players[b].y) >= 39.9,
      `red ${a} and ${b} stacked at ${game.elapsed}`)
  }
  assert.ok(game.players[7].x > game.players[6].x && game.players[7].y < game.players[6].y,
    'weak-side forward opens inside and ahead of the lower-side carrier')
  assert.ok(game.players[9].x < game.players[7].x, 'center remains closer to the blue net')
  assert.ok(game.players[8].x > 330 && game.players[10].x > 330,
    'both red defensemen hold distinct point lanes')
})

test('a blue turnover starts a stable 2-1-2 red forecheck', () => {
  let game = startGame(newGame('free'))
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 230, y: 315 } : p) }
  game = act(game, 'poke')
  game = runUntil(game, g => g.elapsed >= 6)
  assert.deepEqual(game.forecheck, { f1: 6, f2: 9, f3: 7 })
  const { f1, f2, f3 } = game.forecheck
  assert.ok(Math.hypot(game.players[f1].x - game.players[2].x,
    game.players[f1].y - game.players[2].y) < 90, 'F1 pressures the carrier')
  assert.ok(game.players[f2].y < game.players[f3].y, 'F2 covers the wall; F3 stays middle')
  assert.ok(game.players[8].x > game.players[f3].x && game.players[10].x > game.players[f3].x,
    'red defensemen stay behind the forwards at the point')
})

test('early turnover supersedes both reads, even if the shift ends before the handoff', () => {
  let game = startGame(newGame('free'))
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 230, y: 315 } : p) }
  game = act(game, 'poke')
  assert.deepEqual(game.decisions.map(d => d.result), ['superseded', 'superseded'])
  game = act(game, 'shoot')
  game = runUntil(game, g => g.pause === 'ended')
  assert.deepEqual(game.decisions.map(d => d.result), ['superseded', 'superseded'])
  assert.equal(game.coverage.second.eligible, 0)
})

test('guided play does not pause for a handoff after an early turnover', () => {
  let game = startGame(newGame())
  game = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 230, y: 315 } : p) }
  game = act(game, 'poke')
  game = runUntil(game, g => g.elapsed >= 3)
  assert.equal(game.pause, null)
  assert.equal(game.taught, true)
  assert.deepEqual(game.decisions.map(d => d.result), ['superseded', 'superseded'])
})

test('a shift ending without a red possession window marks reads unobserved', () => {
  let game = startGame(newGame('free'))
  game = { ...game, puck: { x: 300, y: 300, owner: null, flight: null }, endAt: 1 }
  game = runUntil(game, g => g.pause === 'ended')
  assert.deepEqual(game.decisions.map(d => d.result), ['unobserved', 'unobserved'])
})

test('freer mode records reads without live coaching feedback', () => {
  let game = runUntil(startGame(newGame('free')), g => g.decisions.length === 1)
  assert.equal(game.feedback, null)
  assert.equal(game.event, 'Live play')
  game = runUntil(game, g => g.decisions.length === 2)
  assert.equal(game.feedback, null)
  assert.doesNotMatch(game.message, /contain|read|net front/i)
})

test('portrait and landscape camera keep player, puck, and partner visible', () => {
  const game = newGame('free')
  for (const viewport of [{ width: 320, height: 640 }, { width: 667, height: 375 }]) {
    let camera = cameraFor(game, viewport)
    for (const point of [game.players[2], game.players[4], game.puck]) {
      assert.ok(point.x >= camera.x && point.x <= camera.x + camera.width)
      assert.ok(point.y >= camera.y && point.y <= camera.y + camera.height)
    }
    const separated = { ...game, players: game.players.map(p => p.id === 2 ? { ...p, x: 1050, y: 610 } : p),
      puck: { ...game.puck, x: 150, y: 180 } }
    camera = cameraFor(separated, viewport)
    for (const point of [separated.players[2], separated.players[4], separated.puck]) {
      assert.ok(point.x >= camera.x && point.x <= camera.x + camera.width)
      assert.ok(point.y >= camera.y && point.y <= camera.y + camera.height)
    }
  }
})
