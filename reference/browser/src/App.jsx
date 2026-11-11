import React, { useEffect, useRef, useState } from 'react'
import rinkUrl from '../visual-design/rink-base-clean.svg'
import reviewSheetUrl from '../visual-design/s04-v2-390-review-sheet.svg'
import { cameraFor, cameraViewBox } from './camera.js'
import { SHIFT_LENGTH, act, chooseIntent, nearestReplayFrame, newGame, setTarget, startGame, steerWithKeys, stopAtPlayer, tick, togglePause } from './game.js'

const clock = seconds => `0:${String(Math.ceil(seconds)).padStart(2, '0')}`
const screenSize = () => ({ width: window.innerWidth, height: window.innerHeight })
function createShift(mode) {
  const requested = new URLSearchParams(window.location.search).get('seed')
  const seed = requested !== null && /^\d{1,10}$/.test(requested) && Number(requested) <= 0xffffffff
    ? Number(requested) : window.crypto.getRandomValues(new Uint32Array(1))[0]
  return newGame(mode, seed)
}
const movementKeys = ['w', 'a', 's', 'd', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']
const starPoints = '0,-26 7,-9 25,-8 11,4 16,23 0,13 -16,23 -11,4 -25,-8 -7,-9'

function Player({ player, selected, possession, partnerSet }) {
  const goalie = player.slot === 5
  const partner = player.id === 4
  return <g transform={`translate(${player.x} ${player.y})`}
    className={`skater ${player.team === 0 ? 'home' : 'away'} ${selected ? 'selected' : ''}`}>
    {partner && partnerSet && <circle className="partner-ring" r="27" />}
    {selected ? <polygon className="active-star" points={starPoints} />
      : goalie ? <rect className="goalie-shape" x="-21" y="-21" width="42" height="42" rx="9" />
      : player.team === 1 ? <rect className="skater-shape" x="-19" y="-19" width="38" height="38" rx="8" />
        : <circle className="skater-shape" r="19" />}
    <text textAnchor="middle" dominantBaseline="central">{selected ? 'Y' : player.role}</text>
    {possession && <circle className="possession-ring" r="25" />}
  </g>
}

function ShiftReview({ game, onReview }) {
  if (game.mode === 'free' && game.pause !== 'ended')
    return <p className="review-pending">Key moments will appear after the shift.</p>
  const labels = ['Hold net front until RD sets', 'Cover Red C near the net after RD sets']
  return <div className="review-list">
    {labels.map((label, i) => {
      const decision = game.decisions.find(read => read.number === i + 1)
      const assessed = ['met', 'missed'].includes(decision?.result)
      const notes = [
        { met: 'You protected the area in front of the net while RD returned.', missed: 'Stay near the net until RD is in position.' },
        { met: 'You stayed with Red C while RD handled the carrier.', missed: 'Stay with Red C near the net. Use Tie up when within stick reach.' },
      ]
      return <div className="review-row" key={label}>
        <span className="review-number">{i + 1}</span>
        <span>{label}</span>
        <strong className={decision?.result === 'met' ? 'met' : ''}>
          {decision ? ({ met: 'Covered', missed: 'Needs review', superseded: 'Not observed', unobserved: 'Not observed' }[decision.result]) : 'Pending'}
        </strong>
        {decision && <small>{notes[i][decision.result] ?? decision.reason}</small>}
        {onReview && assessed && <button className="button secondary review-jump"
          onClick={() => onReview(decision)}>Review moment {decision.number}</button>}
      </div>
    })}
  </div>
}

export default function App() {
  const [game, setGame] = useState(() => createShift())
  const [viewport, setViewport] = useState(screenSize)
  const [replayIndex, setReplayIndex] = useState(null)
  const svgRef = useRef(null)
  const summaryRef = useRef(null)
  const replayRef = useRef(null)
  useEffect(() => {
    if (game.pause === 'ended') summaryRef.current?.focus()
  }, [game.pause])
  const keys = useRef(new Set())
  useEffect(() => {
    const timer = window.setInterval(() => setGame(current => {
      let next = current
      if (current.running && keys.current.size) next = steerWithKeys(current, keys.current)
      return tick(next)
    }), 100)
    return () => window.clearInterval(timer)
  }, [])
  useEffect(() => {
    const resize = () => setViewport(screenSize())
    const down = event => {
      const key = event.key.length === 1 ? event.key.toLowerCase() : event.key
      if (movementKeys.includes(key)) {
        if (!['INPUT', 'TEXTAREA'].includes(event.target.tagName)) event.preventDefault()
        keys.current.add(key)
        setGame(g => g.running ? steerWithKeys(g, keys.current) : g)
      }
      if (event.code === 'Space' && !event.repeat) {
        event.preventDefault()
        setGame(g => g.pause === 'ready' ? startGame(g) : togglePause(g))
      }
      if (key === '1') setGame(g => act(g, 'poke'))
      if (key === '2') setGame(g => act(g, 'pass'))
      if (key === '3') setGame(g => act(g, 'shoot'))
      if (key === '4') setGame(g => act(g, 'tie'))
    }
    const up = event => {
      const key = event.key.length === 1 ? event.key.toLowerCase() : event.key
      keys.current.delete(key)
      if (movementKeys.includes(key))
        setGame(g => movementKeys.some(held => keys.current.has(held))
          ? steerWithKeys(g, keys.current) : stopAtPlayer(g))
    }
    const blur = () => {
      if (movementKeys.some(key => keys.current.has(key))) setGame(g => stopAtPlayer(g))
      keys.current.clear()
    }
    window.addEventListener('resize', resize)
    window.addEventListener('keydown', down)
    window.addEventListener('keyup', up)
    window.addEventListener('blur', blur)
    return () => {
      window.removeEventListener('resize', resize)
      window.removeEventListener('keydown', down)
      window.removeEventListener('keyup', up)
      window.removeEventListener('blur', blur)
    }
  }, [])

  const moveTo = event => {
    if (!game.running) return
    const svg = svgRef.current
    if (!svg) return
    const point = svg.createSVGPoint()
    point.x = event.clientX
    point.y = event.clientY
    const local = point.matrixTransform(svg.getScreenCTM().inverse())
    setGame(g => setTarget(g, local.x, local.y))
  }
  const replayOpen = game.pause === 'ended' && replayIndex !== null
  const frame = replayOpen ? game.history[Math.min(replayIndex, game.history.length - 1)] : null
  const displayGame = frame ? { ...game, players: frame.players, puck: frame.puck,
    target: frame.target, tieUp: frame.tieUp, slotPassAt: frame.slotPassAt } : game
  const slotWarning = displayGame.slotPassAt != null
  const redCenter = displayGame.players[9]
  const selected = displayGame.players[2]
  const camera = cameraFor(displayGame, viewport)
  const viewBox = cameraViewBox(camera)
  const owner = displayGame.players[displayGame.puck.owner]
  const status = replayOpen ? 'Reviewing replay' : game.pause === 'teaching' ? 'Teaching pause' : game.pause === 'ended' ? 'Shift complete'
    : game.running ? 'Shift live' : game.pause === 'manual' ? 'Paused' : 'Ready'
  const compact = camera.width !== 1260 || camera.height !== 650
  const reset = (mode = game.mode) => { setReplayIndex(null); setGame(createShift(mode)) }
  const resetAndStart = mode => {
    setReplayIndex(null)
    setGame(startGame(createShift(mode)))
    svgRef.current?.scrollIntoView({ block: 'nearest' })
  }
  const openReplay = (index = 0) => {
    setReplayIndex(index)
    requestAnimationFrame(() => replayRef.current?.focus())
  }
  const jumpToRead = decision => openReplay(nearestReplayFrame(game.history, decision.replayAt ?? decision.at))
  const reviewShift = () => {
    const read = game.decisions.find(item => item.result === 'missed') ??
      game.decisions.find(item => item.result === 'met')
    if (read) jumpToRead(read)
    else openReplay()
  }
  const trail = replayOpen ? game.history.slice(0, replayIndex + 1).map(item => `${item.players[2].x},${item.players[2].y}`).join(' ') : ''
  const passIndex = replayOpen ? game.history.findIndex(item =>
    ['Red feeds the slot', 'Red cycles high'].includes(item.event)) : -1
  const warningIndex = passIndex >= 0 && game.history[passIndex].event === 'Red feeds the slot'
    ? game.history.findIndex(item => item.slotPassAt != null) : -1
  const receptionIndex = passIndex < 0 ? -1 : game.history.findIndex((item, index) =>
    index > passIndex && ['Pass received', 'Pass denied', 'Pass intercepted'].includes(item.event))
  const shotResultIndex = receptionIndex < 0 || game.history[receptionIndex].event !== 'Pass received'
    ? -1 : game.history.findIndex((item, index) => index > receptionIndex && ['Save', 'Goal'].includes(item.event))

  return <div className="app-shell">
    <header className="topbar">
      <div className="brand"><span className="brand-mark">SH<span>!</span></span>
        <div><strong>SHIFT HAPPENS!</strong><small>Defensive exchange prototype</small></div></div>
      <div className="top-actions"><span className="build-tag">Arcade Ice · playtest</span>
        <button className="text-button" onClick={() => reset()}>Reset shift</button></div>
    </header>
    <main className="layout">
      <section className="game-panel" aria-label="Hockey game">
        <div className="game-heading"><div><h1>{game.mode === 'guided' ? 'Protect the middle.' : 'Take the ice.'}</h1>
          <p>Control blue LD through a live 5-on-5 defensive exchange.</p></div>
          <div className={`live-indicator ${game.running ? 'on' : ''}`}><i />{status}</div></div>
        <div className="mode-switch" role="group" aria-label="Practice mode">
          <span>Practice mode</span>
          <button className={game.mode === 'guided' ? 'active' : ''} aria-pressed={game.mode === 'guided'}
            onClick={() => reset('guided')}>Guided · one teaching pause</button>
          <button className={game.mode === 'free' ? 'active' : ''} aria-pressed={game.mode === 'free'}
            onClick={() => reset('free')}>Freer · review afterward</button>
        </div>
        <div className="scoreboard" aria-label={`Score: Blue ${frame?.score[0] ?? game.score[0]}, Red ${frame?.score[1] ?? game.score[1]}. ${clock(frame ? SHIFT_LENGTH - frame.time : game.seconds)} remaining.`}>
          <div className="team-score"><span className="team-dot blue" />BLUE <strong>{frame?.score[0] ?? game.score[0]}</strong></div>
          <div className="time"><span>{replayOpen ? 'REPLAY CLOCK' : 'SHIFT CLOCK'}</span><strong>{clock(frame ? SHIFT_LENGTH - frame.time : game.seconds)}</strong></div>
          <div className="team-score red-team"><strong>{frame?.score[1] ?? game.score[1]}</strong> RED<span className="team-dot red" /></div>
        </div>
        <div className={`rink-frame ${compact ? 'compact-rink' : ''}`}>
          <svg ref={svgRef} viewBox={viewBox} role="img"
            aria-label="Overhead hockey rink. Y is your blue left defenseman, RD is your blue right defenseman, red squares are opponents. Drag or tap the ice to skate."
            onPointerDown={event => { event.currentTarget.setPointerCapture(event.pointerId); moveTo(event) }}
            onPointerMove={event => { if (event.buttons || event.pointerType === 'touch') moveTo(event) }}>
            <image href={rinkUrl} x="0" y="70" width="1260" height="650" />
            <path className="goal-net" d="M99 365L61 374V420L99 429 M1163 365L1201 374V420L1163 429" />
            {game.mode === 'guided' && game.feedback && <ellipse className={`teaching-zone ${game.feedback.kind}`}
              cx={game.feedback.focus.x} cy={game.feedback.focus.y}
              rx={game.decisions.length === 1 ? 90 : 58} ry={game.decisions.length === 1 ? 98 : 58} />}
            {replayOpen && <polyline className="replay-trail" points={trail} />}
            {replayOpen && game.decisions.filter(decision => ['met', 'missed'].includes(decision.result)).map(decision => {
              const mark = game.history[nearestReplayFrame(game.history, decision.replayAt ?? decision.at)].players[2]
              return <g key={decision.number} className="replay-marker" transform={`translate(${mark.x} ${mark.y})`}>
                <circle r="13" /><text textAnchor="middle" dominantBaseline="central">{decision.number}</text>
              </g>
            })}
            {!replayOpen && <circle className="move-target" cx={game.target.x} cy={game.target.y} r="11" />}
            {slotWarning && <g className="slot-pass-path" aria-hidden="true">
              <line x1={displayGame.puck.x} y1={displayGame.puck.y} x2={redCenter.x} y2={redCenter.y} />
              <circle cx={redCenter.x} cy={redCenter.y} r="32" />
            </g>}
            {displayGame.tieUp && <g className="tie-up-visual" aria-label="Red C tied up">
              <line x1={displayGame.players[2].x} y1={displayGame.players[2].y}
                x2={displayGame.players[displayGame.tieUp.opponent].x}
                y2={displayGame.players[displayGame.tieUp.opponent].y} />
              <circle cx={displayGame.players[displayGame.tieUp.opponent].x}
                cy={displayGame.players[displayGame.tieUp.opponent].y} r="29" />
            </g>}
            {displayGame.players.map(player => <Player key={player.id} player={player}
              selected={player.id === 2} partnerSet={game.mode === 'guided' && (frame?.partnerSet ?? game.partnerSet)} possession={player.id === displayGame.puck.owner} />)}
            {displayGame.tieUp && <text className="tie-up-label"
              x={displayGame.players[displayGame.tieUp.opponent].x}
              y={displayGame.players[displayGame.tieUp.opponent].y - 36}
              textAnchor="middle">TIED UP</text>}
            <circle className="puck-shadow" cx={displayGame.puck.x + 3} cy={displayGame.puck.y + 4} r="10" />
            <circle className="puck" cx={displayGame.puck.x} cy={displayGame.puck.y} r="9" />
          </svg>
          {slotWarning && <div className="slot-pass-warning" role="status">Pass coming to Red C</div>}
          {compact && <div className="rink-overview" aria-label="Full rink overview showing the camera, player, and puck">
            <svg viewBox="0 70 1260 650" role="img" aria-label="Full rink overview">
              <image href={rinkUrl} x="0" y="70" width="1260" height="650" />
              <rect className="overview-camera" x={camera.x} y={camera.y} width={camera.width} height={camera.height} />
              <circle className="overview-player" cx={selected.x} cy={selected.y} r="24" />
              <circle className="overview-puck" cx={displayGame.puck.x} cy={displayGame.puck.y} r="19" />
            </svg><span>FULL RINK</span></div>}
          {game.mode === 'guided' && game.feedback && game.running && <div className={`consequence ${game.feedback.kind}`} role="status">
            <strong>{game.feedback.title}</strong><span>{game.feedback.body}</span><small>Draft coaching read</small></div>}
          {game.pause === 'teaching' && <div className="rink-overlay teaching-overlay">
            <span className="overlay-kicker">TEACHING PAUSE · READ 1 OF 2</span>
            <strong>RD is returning. What will you protect?</strong>
            <p>Choose an intent, then skate it when play resumes. The action counts, not the tap.</p>
            <div className="choice-row">
              <button className="button primary" onClick={() => setGame(g => chooseIntent(g, 'hold'))}>Hold net front</button>
              <button className="button secondary" onClick={() => setGame(g => chooseIntent(g, 'chase'))}>Chase the carrier</button>
            </div>
          </div>}
          {['ready', 'manual'].includes(game.pause) && <div className="rink-overlay">
            <strong>{game.pause === 'manual' ? 'Shift paused' : 'Ready for the shift?'}</strong>
            <span>{game.pause === 'manual' ? 'The puck, players, goalies, and clock are frozen.' :
                game.mode === 'guided' ? 'You control Y at LD. Your RD is returning from the goal-line corner.' : 'You control Y at LD. Red starts low on your side with the puck.'}</span>
            <button className="button primary" onClick={() =>
              setGame(g => g.pause === 'ready' ? startGame(g) : togglePause(g))}>
              {game.pause === 'manual' ? 'Resume shift' : 'Start shift'}
            </button>
          </div>}
        </div>
        {replayOpen && <div ref={replayRef} tabIndex={-1} className="replay-controls" aria-label="Shift replay controls">
          <div className="replay-heading"><strong>Shift replay</strong><span>{frame.time.toFixed(1)}s of {game.elapsed.toFixed(1)}s</span></div>
          <input type="range" min="0" max={game.history.length - 1} value={replayIndex}
            onChange={event => setReplayIndex(Number(event.target.value))} aria-label="Replay time" />
          <div className="replay-actions">
            <button className="button secondary" onClick={() => setReplayIndex(0)}>From start</button>
            {warningIndex >= 0 && <button className="button secondary" onClick={() => setReplayIndex(warningIndex)}>Pass warning</button>}
            {passIndex >= 0 && <button className="button secondary" onClick={() => setReplayIndex(passIndex)}>{game.history[passIndex].event}</button>}
            {receptionIndex >= 0 && <button className="button secondary" onClick={() => setReplayIndex(receptionIndex)}>{game.history[receptionIndex].event}</button>}
            {shotResultIndex >= 0 && <button className="button secondary" onClick={() => setReplayIndex(shotResultIndex)}>{game.history[shotResultIndex].event}</button>}
            {game.decisions.filter(decision => ['met', 'missed'].includes(decision.result)).map(decision =>
              <button key={decision.number} className="button secondary" onClick={() => jumpToRead(decision)}>Read {decision.number}</button>)}
            <button className="button secondary" onClick={() => {
              setReplayIndex(null)
              summaryRef.current?.focus()
            }}>Close replay</button>
          </div>
          <p>Shift seed: {game.seed}. Replay captures key puck plays and samples positions about every half second.</p>
        </div>}
        <div className="rink-caption"><span><b className="legend-dot blue" /> Y = you</span>
          <span><b className="legend-dot blue partner-key" /> RD = right defense</span>
          <span><b className="legend-dot red" /> Red = opponents</span>
          <span><b className="legend-dot puck-key" /> Puck</span><span>Defending LEFT</span></div>
        {game.pause === 'ended' && <section className="shift-summary" ref={summaryRef} tabIndex={-1} aria-labelledby="shift-outcome">
          <h2 id="shift-outcome">{game.outcome ?? 'Shift time expired'}</h2>
          <p>Blue {game.score[0]} · Red {game.score[1]} · Draft coaching feedback</p>
          <ShiftReview game={game} onReview={jumpToRead} />
          {game.tieUps.length > 0 && <p>Tie-ups used · {game.tieUps.length}</p>}
          <div className="replay-actions">
            <button className="button primary" onClick={reviewShift}>Review shift</button>
            <button className="button secondary" onClick={() => resetAndStart('guided')}>Retry guided</button>
            <button className="button secondary" onClick={() => resetAndStart('free')}>Try freer practice</button>
          </div>
        </section>}
        {game.pause !== 'ended' && <div className={`action-row ${game.running ? 'mobile-live-controls' : ''}`}>
          <button className="button secondary" onClick={() => setGame(g => g.pause === 'ready' ? startGame(g) : togglePause(g))}
            disabled={game.pause === 'teaching' || game.pause === 'ended'}>
            {game.running ? 'Pause' : game.pause === 'manual' ? 'Resume' : 'Start'} <kbd>Space</kbd></button>
          <button className="button action" onClick={() => setGame(g => act(g, 'poke'))} disabled={!game.running}>Stick <kbd>1</kbd></button>
          <button className="button action" onClick={() => setGame(g => act(g, 'pass'))} disabled={!game.running}>Pass <kbd>2</kbd></button>
          <button className="button action" onClick={() => setGame(g => act(g, 'shoot'))} disabled={!game.running}>Clear <kbd>3</kbd></button>
          <button className="button action tie-action" onClick={() => setGame(g => act(g, 'tie'))}
            disabled={!game.running} aria-label="Tie up Red C with stick">Tie up <kbd>4</kbd></button>
        </div>}
      </section>
      <aside className="side-panel" aria-label="Shift details">
        <div className="side-section"><h2>On your shift</h2>
          <div className="player-card"><svg className="player-avatar active-player-avatar" viewBox="0 0 52 52"
            aria-label="Active player Y"><polygon points={starPoints} transform="translate(26 26)" />
            <text x="26" y="28" textAnchor="middle" dominantBaseline="central">Y</text></svg>
            <div><small>YOU CONTROL</small>
            <strong>Blue LD</strong><span>{owner ? `${owner.team === 0 ? 'Blue' : 'Red'} has the puck` : 'Puck in motion'}</span></div></div>
          {displayGame.tieUp && <p className="tie-up-status">Red C tied up · stick lane blocked</p>}
          <p className="tip">{game.mode === 'guided'
            ? 'Skate with WASD, arrows, or by dragging on the rink. Hold inside while RD returns, then cover Red C near the net as RD handles the puck side. Tie up Red C when within stick reach. Your wings rotate high and into the slot while C supports low. Coaching positions are draft.'
            : 'Skate with WASD, arrows, or by dragging on the rink. Automatic coaching pauses and tactical cues are off; review key moments afterward.'}</p></div>
        <div className="side-section"><div className="section-line"><h2>Play by play</h2><span>{frame?.event ?? game.event}</span></div>
          <p className="event-message" aria-live="polite">{frame?.message ?? game.message}</p></div>
        {game.pause !== 'ended' && <div className="side-section coaching"><h2>Draft shift review</h2><ShiftReview game={game} />
          {game.tieUps.length > 0 && <p>{game.tieUps.length} coverage tie-up{game.tieUps.length === 1 ? '' : 's'} used this shift.</p>}
          <p>These positions and checks await David's coaching review. This prototype awards no mastery or tokens.</p>
          <a href={reviewSheetUrl} target="_blank" rel="noreferrer">View S04 review sheet</a></div>}
      </aside>
    </main>
    <footer>Experimental S04 slice · five skaters and a goalie per side · simplified hockey rules · no validated scoring</footer>
  </div>
}
