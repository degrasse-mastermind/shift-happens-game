export const RINK = { left: 48, right: 1214, top: 118, bottom: 676 }
export const SHIFT_LENGTH = 30
const STEP = 0.1
const TIE_UP_REACH = 75
const TIE_UP_BREAK = 90
const TIE_UP_DURATION = 2.5
const TIE_UP_COOLDOWN = 3.5
const SLOT_PASS_WARNING = 1.5
// Red has established the attacking zone. Blue's right defenseman starts low
// by his goal-line corner and recovers to net front while LD protects inside.
const HOME = [[310,205],[310,585],[185,360],[250,340],[130,600],[105,397]]
const AWAY = [[230,315],[230,500],[350,235],[220,410],[350,525],[1163,397]]
const HOME_ROLES = ['LW','RW','LD','C','RD','G']
const AWAY_ROLES = ['RW','LW','RD','C','LD','G']
// Experimental thresholds for playtesting only; David has not approved tactical geometry.
export const DRAFT_READ_RULES = {
  netFront: { maxX: 250, minY: 315, maxY: 485, minCoverage: 0.7 },
  redCenter: { minX: 135, maxOutsideX: 40, maxDistance: 82, minCoverage: 0.45 },
  secondReadAt: 18,
}
const clamp = (n,a,b) => Math.max(a,Math.min(b,n))
const distance = (a,b) => Math.hypot(a.x-b.x,a.y-b.y)
const distanceToSegment = (p,a,b) => {
  const dx=b.x-a.x,dy=b.y-a.y
  const lengthSquared=dx*dx+dy*dy
  const fraction=lengthSquared?clamp(((p.x-a.x)*dx+(p.y-a.y)*dy)/lengthSquared,0,1):0
  return distance(p,{x:a.x+fraction*dx,y:a.y+fraction*dy})
}
const move = (a,b,s) => {
  const d=distance(a,b)
  return d<=s ? {x:b.x,y:b.y} : {x:a.x+(b.x-a.x)*s/d,y:a.y+(b.y-a.y)*s/d}
}
const skateToward = (p,goal,maxStep) => {
  const dx=goal.x-p.x,dy=goal.y-p.y,d=Math.hypot(dx,dy)
  const speed=Math.min(maxStep,d*.45)
  const vx=(p.vx||0)*.55+(d?dx/d*speed:0)*.45
  const vy=(p.vy||0)*.55+(d?dy/d*speed:0)*.45
  if(d<1.5&&Math.hypot(vx,vy)<.35)return{...p,x:goal.x,y:goal.y,vx:0,vy:0}
  return{...p,x:clamp(p.x+vx,RINK.left,RINK.right),y:clamp(p.y+vy,RINK.top,RINK.bottom),vx,vy}
}
export const replayFrame = game => ({
  time: game.elapsed,
  players: game.players.map(({ id, team, slot, role, x, y }) => ({ id, team, slot, role, x, y })),
  puck: { x: game.puck.x, y: game.puck.y, owner: game.puck.owner },
  target: { ...game.target },
  partnerSet: game.partnerSet,
  score: [...game.score],
  event: game.event,
  message: game.message,
  slotPassAt: game.slotPassAt,
  tieUp: game.tieUp ? { ...game.tieUp } : null,
})
const recordFrame = game => ({ ...game, history: [...game.history, replayFrame(game)] })
export const nearestReplayFrame = (history, time) =>
  history.reduce((best, frame, index) =>
    Math.abs(frame.time - time) < Math.abs(history[best].time - time) ? index : best, 0)

export function newGame(mode='guided', seed=0) {
  const game = {
    players:[HOME,AWAY].flatMap((spots,team)=>spots.map(([x,y],slot)=>({id:team*6+slot,team,slot,
      role:(team===0?HOME_ROLES:AWAY_ROLES)[slot],x,y,vx:0,vy:0}))),
    selected:2,target:{x:185,y:360},puck:{x:213,y:324,owner:6,flight:null},
    mode,seed,score:[0,0],seconds:SHIFT_LENGTH,elapsed:0,running:false,started:false,pause:'ready',
    taught:false,intent:null,decisions:[],partnerSet:false,feedback:null,
    coverage:{first:{eligible:0,covered:0},second:{eligible:0,covered:0}},
    event:'Ready',message:mode==='guided'?'Red has the puck low on the LD-side circle. Protect inside while RD returns.':'Red has the puck low on the LD-side circle. Read the developing play.',
    passMade:false,slotPassAt:null,shotTaken:false,endAt:null,cornerExitSeen:false,forecheck:null,
    tieUp:null,tieUpReadyAt:0,tieUps:[],
  }
  return { ...game, history: [replayFrame(game)] }
}
export const setTarget=(g,x,y)=>({...g,target:{x:clamp(x,RINK.left,RINK.right),y:clamp(y,RINK.top,RINK.bottom)}})
export const stopAtPlayer=g=>setTarget(g,g.players[g.selected].x,g.players[g.selected].y)
export function steerWithKeys(g,keys) {
  const dx=Number(keys.has('d')||keys.has('ArrowRight'))-Number(keys.has('a')||keys.has('ArrowLeft'))
  const dy=Number(keys.has('s')||keys.has('ArrowDown'))-Number(keys.has('w')||keys.has('ArrowUp'))
  if(!dx&&!dy)return stopAtPlayer(g)
  const player=g.players[g.selected]
  const reach=60/Math.hypot(dx,dy)
  return setTarget(g,player.x+dx*reach,player.y+dy*reach)
}
export const startGame=g=>g.pause==='ended'?g:{...g,started:true,running:true,pause:null,event:'Live play'}
export const togglePause=g=>['teaching','ended','ready'].includes(g.pause)?g:
  g.running?{...g,running:false,pause:'manual',event:'Paused'}:{...g,running:true,pause:null,event:'Live play'}
export const chooseIntent=(g,intent)=>g.pause!=='teaching'||!['hold','chase'].includes(intent)?g:{
  ...g,intent,running:true,pause:null,event:'Play resumed',
  message:intent==='hold'?'Skate the read: protect the inside as RD returns.':'Skate the read: follow the puck carrier.',
}

function blueDefensiveTarget(p,g) {
  const puck=g.puck
  const behindNet=puck.owner===6&&puck.x<105
  const upper=behindNet||puck.y<397
  const redCenter=g.players[9]
  const strongSideCarrier=g.partnerSet&&puck.owner!=null&&g.players[puck.owner]?.team===1&&
    !behindNet&&puck.x<300&&puck.y>430
  // Box + 1: the puck-side wing takes the point lane, the far wing tucks into
  // the high slot, C supports low, and RD protects the net-front layer.
  // Y (LD) remains player-controlled; these targets do not score the reads.
  if(p.id===0||p.id===1) {
    const strongSide=p.id===(upper?0:1)
    return strongSide?{
      x:clamp(puck.x+45,290,315),
      y:upper?clamp(puck.y-50,225,245):clamp(puck.y+50,549,569),
    }:{x:clamp(puck.x-35,270,290),y:upper?410:384}
  }
  switch(p.id) {
    case 3: return behindNet?{x:205,y:397}:{x:clamp(puck.x+45,245,280),
      y:upper?clamp(puck.y+56,360,405):clamp(puck.y-56,390,445)}
    case 4: return behindNet?{x:155,y:455}:strongSideCarrier?
      {x:clamp(puck.x-15,155,185),y:clamp(puck.y-23,450,485)}:
      {x:clamp(redCenter.x-40,135,205),y:redCenter.y+25}
  }
}
function redWrapTarget(t) {
  if(t<3)return{x:140,y:290}    // LD-side corner
  if(t<6)return{x:70,y:335}     // enter behind the upper post
  if(t<9)return{x:70,y:455}     // cross behind the goal
  if(t<12)return{x:155,y:490}   // emerge on the RD side
  return{x:205,y:475}           // lower-circle possession
}
function redAttackingTarget(p,g,t) {
  const puck=g.puck
  const behindNet=puck.owner===6&&puck.x<105
  const upper=behindNet||puck.y<397
  switch(p.id) {
    case 6: return puck.owner===6?redWrapTarget(t):{x:upper?210:225,y:upper?330:465}
    case 7: return upper?{x:clamp(puck.x+35,220,270),y:clamp(puck.y+160,475,510)}
      :{x:clamp(puck.x+60,220,280),y:clamp(puck.y-55,405,445)}
    case 8: return puck.owner===8?{x:330,y:265}
      :{x:clamp(puck.x+155,345,390),y:upper?clamp(puck.y-90,225,265):270}
    case 9: return (g.seed&1)!==0&&g.cornerExitSeen&&puck.owner===6&&puck.y>430?
      {x:204,y:402}:behindNet?{x:160,y:397}:upper?
      {x:clamp(puck.x-45,160,190),y:clamp(puck.y+75,395,420)}
      :{x:clamp(puck.x-30,155,185),y:clamp(puck.y-65,395,430)}
    case 10:return puck.owner===10?{x:340,y:510}
      :{x:clamp(puck.x+145,340,385),y:upper?525:clamp(puck.y+60,505,545)}
  }
}
function blueTransitionTarget(p,g) {
  const puck=g.puck
  const upper=puck.y<397
  switch(p.id) {
    case 0: return{x:clamp(puck.x+90,325,520),y:205}
    case 1: return{x:clamp(puck.x+80,325,510),y:575}
    case 3: return{x:clamp(puck.x+55,265,460),y:clamp(puck.y+(upper?70:-70),290,500)}
    case 4: return{x:clamp(puck.x-60,160,340),y:clamp(puck.y+(upper?45:-45),310,485)}
  }
}
function blueTransition(g) {
  const owner=g.puck.owner==null?null:g.players[g.puck.owner]
  return owner?.team===0&&owner.slot!==5||g.puck.flight?.kind==='clear'||
    g.puck.flight?.kind==='pass'&&g.players[g.puck.flight.receiver]?.team===0
}
function blueThreat(g) {
  const receiver=g.puck.flight?.kind==='pass'?g.players[g.puck.flight.receiver]:null
  return receiver?.team===0?receiver:g.puck.owner!=null&&g.players[g.puck.owner]?.team===0?
    g.players[g.puck.owner]:g.puck
}
function assignForecheck(g) {
  const threat=blueThreat(g)
  const [f1,f2,f3]=[6,7,9].sort((a,b)=>distance(g.players[a],threat)-distance(g.players[b],threat))
  return{f1,f2,f3}
}
function redForecheckTarget(p,g) {
  const threat=blueThreat(g)
  const upper=threat.y<397
  const inside=upper?1:-1
  const {f1,f2,f3}=g.forecheck||assignForecheck(g)
  if(p.id===f1)return{x:clamp(threat.x+22,155,380),
    y:clamp(threat.y+inside*28,175,615)}
  if(p.id===f2)return{x:clamp(threat.x+82,235,405),
    y:upper?clamp(threat.y-45,185,300):clamp(threat.y+45,495,610)}
  if(p.id===f3)return{x:clamp(threat.x+120,285,410),y:upper?430:365}
  const pointX=clamp(threat.x+160,355,435)
  if(p.id===8)return{x:pointX,y:upper?250:270}
  if(p.id===10)return{x:pointX-12,y:upper?525:545}
}
function aiTarget(p,g,t) {
  const puck=g.puck
  if(p.id===5)return{x:105,y:clamp(puck.flight?.kind==='shot'&&puck.flight.team===1?puck.flight.y:puck.y,365,429)}
  if(p.id===11)return{x:1163,y:clamp(puck.flight?.kind==='shot'&&puck.flight.team===0?puck.flight.y:puck.y,365,429)}
  const blueHasControl=blueTransition(g)
  if(p.team===0)return blueHasControl?blueTransitionTarget(p,g):blueDefensiveTarget(p,g)
  return blueHasControl?redForecheckTarget(p,g):redAttackingTarget(p,g,t)
}
function positionPlayers(g,t) {
  const players=g.players.map(p=>{
    const goal=p.id===g.selected?g.target:aiTarget(p,g,t)
    let speed=p.id===g.selected?5.3:p.slot===5?(g.puck.flight?.kind==='shot'?1.2:2):
      p.id===4?2.1:p.id===3?3.2:3
    if(g.tieUp?.opponent===p.id)speed=.35
    return p.id===g.selected||p.slot===5?{...p,...move(p,goal,speed)}:skateToward(p,goal,speed)
  })
  // Keep red teammates from stacking on the same spot while they rotate.
  for(let a=6;a<=10;a++)for(let b=a+1;b<=10;b++) {
    const dx=players[b].x-players[a].x,dy=players[b].y-players[a].y
    const separation=Math.hypot(dx,dy)
    if(separation>=40)continue
    const ux=separation?dx/separation:1,uy=separation?dy/separation:0
    const gap=40-separation
    const aShare=g.puck.owner===a?0:g.puck.owner===b?1:.5
    const bShare=1-aShare
    players[a]={...players[a],x:clamp(players[a].x-ux*gap*aShare,RINK.left,RINK.right),
      y:clamp(players[a].y-uy*gap*aShare,RINK.top,RINK.bottom)}
    players[b]={...players[b],x:clamp(players[b].x+ux*gap*bShare,RINK.left,RINK.right),
      y:clamp(players[b].y+uy*gap*bShare,RINK.top,RINK.bottom)}
  }
  return players
}
function recordRead(g,number,result,t,reason='') {
  const first=number===1
  const assessed=result==='met'||result==='missed'
  const success=result==='met'
  const coaching=g.mode==='guided'&&assessed
  return{
    ...g,decisions:[...g.decisions,{number,result,at:t,reason,
      replayAt:g.coverage[first?'first':'second'][success?'lastCovered':'lastOpen']??t}],
    feedback:coaching?{
      title:first?(success?'Inside lane stayed covered':'Inside lane opened'):(success?'Red C stayed covered':'Red C got free'),
      body:first?(success?'You stayed near the net while RD returned.':'You left net front before RD was set.')
        :(success?'You stayed with Red C near the net while RD handled the puck side.':'Red C was left unmarked near the net while RD handled the puck side.'),
      kind:success?'good':'alert',until:t+3,
      focus:first?{x:190,y:397}:{x:g.players[9].x,y:g.players[9].y},
    }:null,
    event:coaching?(first?'Read 1 of 2':'Read 2 of 2'):g.event,
    message:coaching?(first?'RD has the near post. Pick up Red C near the net.':'Two reads recorded. Keep playing through the shot.'):g.message,
  }
}
function supersedeOpenReads(g,t) {
  let next=g
  let observedSecond=false
  for(const number of [1,2]) {
    if(next.decisions.some(d=>d.number===number))continue
    const second=number===2&&next.partnerSet&&next.coverage.second.eligible>=10
    if(second)observedSecond=true
    const {eligible,covered}=next.coverage.second
    next=second?recordRead(next,2,covered/eligible>=DRAFT_READ_RULES.redCenter.minCoverage?'met':'missed',t,
      'Red possession ended during the Red C coverage window.')
      :recordRead(next,number,'superseded',t,'Red possession ended before this read completed.')
  }
  return{...next,taught:true,feedback:observedSecond?next.feedback:null}
}
function completeReads(g,t) {
  let next=g
  for(const number of [1,2])
    if(!next.decisions.some(d=>d.number===number))
      next=recordRead(next,number,'unobserved',t,'The required live-play window did not occur.')
  return next
}
function redCarrier(g,players) {
  const owner=g.puck.owner
  return owner!=null&&players[owner]?.team===1&&players[owner]?.slot!==5?players[owner]:null
}
function sampleCoverage(g,players) {
  const carrier=redCarrier(g,players)
  if(!carrier)return g
  const you=players[g.selected]
  const field=g.partnerSet?'second':'first'
  const current=g.coverage[field]
  const r=DRAFT_READ_RULES
  const redCenter=players[9]
  const covered=field==='first'
    ?you.x<=r.netFront.maxX&&you.y>=r.netFront.minY&&you.y<=r.netFront.maxY
    :you.x>=r.redCenter.minX&&
      (you.x<=redCenter.x+r.redCenter.maxOutsideX&&
        distance(you,redCenter)<=r.redCenter.maxDistance||g.tieUp?.opponent===redCenter.id)
  return{...g,coverage:{...g.coverage,[field]:{
    ...current,eligible:current.eligible+1,covered:current.covered+Number(covered),
    [covered?'lastCovered':'lastOpen']:g.elapsed,
  }}}
}
function advancePuck(g,players,t) {
  let {puck,score,event,message,shotTaken,endAt,passMade,slotPassAt}=g
  if(puck.flight) {
    const f=puck.flight,dest=f.kind==='pass'?players[f.receiver]:f
    puck={...puck,...move(puck,dest,f.kind==='shot'?18:f.kind==='clear'?21:15)}
    if(f.kind==='pass') {
      const interceptor=players.find(p=>p.team!==players[f.receiver].team&&p.slot!==5&&distance(p,puck)<19)
      if(interceptor) {
        puck={x:interceptor.x,y:interceptor.y,owner:interceptor.id,flight:null}
        event='Pass intercepted';message=`${interceptor.team===0?'Blue':'Red'} breaks up the pass.`
      }
    }
    if(puck.flight&&distance(puck,dest)<19) {
      if(f.kind==='pass') {
        const receiver=players[f.receiver]
        if(g.tieUp?.opponent===receiver.id) {
          puck={x:puck.x,y:puck.y,owner:null,flight:null}
          event='Pass denied';message='Your tie-up prevents a net-front reception.'
        } else {
          puck={x:receiver.x,y:receiver.y,owner:receiver.id,flight:null}
          event='Pass received';message=receiver.team===0?'Blue controls the puck.'
            :receiver.id===9?'Red C receives the feed near the net.':'Red moved the puck across the zone.'
        }
      } else if(f.kind==='clear') {
        puck={x:f.x,y:f.y,owner:null,flight:null}
        event='Zone exit';message='Blue clears the defensive zone.'
        endAt=t+1
      } else {
        const goalie=players[f.team===0?11:5]
        const saved=Math.abs(f.y-goalie.y)<38
        if(saved) {
          puck={x:goalie.x,y:goalie.y,owner:goalie.id,flight:null}
          event='Save';message='Goalie save. The puck is covered; the shift is ending.'
        } else {
          score=score.map((n,team)=>n+(team===f.team?1:0))
          puck={x:f.x,y:f.y,owner:null,flight:null}
          event='Goal';message=f.team===0?'Blue scores.':'Red scores.'
        }
        endAt=t+2
      }
    }
  } else if(puck.owner!=null) {
    const owner=players[puck.owner]
    puck={...puck,x:owner.x+(owner.team===0?17:-17),y:owner.y+9}
    if(owner.team===0&&owner.slot!==5&&owner.id!==g.selected&&owner.x>330&&endAt==null) {
      event='Breakout';message='Blue carries the puck out of the defensive zone.'
      endAt=t+1
    } else if(owner.id===6&&!passMade&&g.partnerSet) {
      const center=players[9]
      // Odd seeds favor the slot when the wrap has opened a short passing lane.
      const slotFeed=(g.seed&1)!==0&&g.cornerExitSeen&&owner.x>=198&&owner.y>430&&
        center.x<225&&Math.abs(center.y-397)<55&&distance(owner,center)<125&&
        players.filter(p=>p.team===0&&p.slot!==5&&p.id!==g.selected)
          .every(p=>distanceToSegment(p,puck,center)>=19)
      if(slotPassAt==null&&slotFeed) {
        slotPassAt=+(t+SLOT_PASS_WARNING).toFixed(1)
        event='Red looks to the slot'
        message='Red C is getting ready for a pass near the net.'
      } else if(slotPassAt!=null?t>=slotPassAt:t>=19) {
        const feedCenter=slotPassAt!=null&&slotFeed
        puck={...puck,owner:null,flight:{kind:'pass',receiver:feedCenter?9:puck.y>397?10:8}}
        passMade=true
        slotPassAt=null
        event=feedCenter?'Red feeds the slot':'Red cycles high'
        message=feedCenter?'The carrier comes out from behind the net and sends a short pass to Red C.'
          :'The carrier moves the puck from the corner to the puck-side point.'
      }
    } else if(owner.id===8&&!shotTaken&&t>=18&&distance(players[g.selected],owner)<90&&!g.weakSidePass) {
      const laneCovered=[players[1],players[3]].some(p=>distanceToSegment(p,owner,players[7])<38)
      puck={...puck,owner:null,flight:{kind:'pass',receiver:laneCovered?10:7}}
      event=laneCovered?'Red switches points':'Red changes sides'
      message=laneCovered?'The lane to the weak-side wing is covered, so Red moves it to the opposite point.'
        :'Pressure at the point sends the puck to the weak side.'
    } else if(owner.team===1&&!shotTaken&&(owner.id===9||t>=22)) {
      puck={x:puck.x,y:puck.y,owner:null,flight:{kind:'shot',x:99,
        y:owner.id===9?(owner.y>397?373:421):owner.id===7?373:390,team:1}}
      shotTaken=true;event='Red shot'
      message=owner.id===9?'Net-front shot by Red C.':owner.id===7?'Weak-side shot on the blue net.':'Point shot through traffic.'
    }
  }
  return{...g,puck,score,event,message,shotTaken,endAt,passMade,slotPassAt:puck.owner===6?slotPassAt:null,
    weakSidePass:g.weakSidePass||(puck.flight?.kind==='pass'&&puck.flight.receiver===7)}
}
export function tick(g) {
  if(!g.running)return g
  if(g.tieUp&&(g.elapsed>=g.tieUp.until||g.puck.owner===g.selected))g={...g,tieUp:null}
  const forecheck=blueTransition(g)?g.forecheck||assignForecheck(g):null
  if(forecheck!==g.forecheck)g={...g,forecheck}
  const elapsed=Math.min(SHIFT_LENGTH,+(g.elapsed+STEP).toFixed(1))
  const seconds=Math.max(0,+(SHIFT_LENGTH-elapsed).toFixed(1))
  if(g.mode==='guided'&&!g.taught&&redCarrier(g,g.players)&&elapsed>=2.5)
    return recordFrame({...g,elapsed,seconds,taught:true,running:false,pause:'teaching',
      event:'Teaching pause',message:'RD is returning. What space will you protect?'})
  const players=positionPlayers(g,elapsed)
  const tieUp=g.tieUp&&distance(players[g.selected],players[g.tieUp.opponent])<=TIE_UP_BREAK?g.tieUp:null
  let next={...g,players,tieUp,elapsed,seconds,feedback:g.feedback?.until>elapsed?g.feedback:null}
  if(!next.partnerSet&&!next.decisions.some(d=>d.number===1))next=sampleCoverage(next,players)
  if(!next.partnerSet&&players[4].x<190&&players[4].y>=400&&players[4].y<=455) {
    next.partnerSet=true
    if(!next.decisions.some(d=>d.number===1)) {
      const {eligible,covered}=next.coverage.first
      next=recordRead(next,1,eligible?covered/eligible>=DRAFT_READ_RULES.netFront.minCoverage?'met':'missed':'unobserved',
        elapsed,eligible?'Coverage sampled until RD became set.':'No red puck carrier during the first read.')
    }
  }
  if(!next.cornerExitSeen&&g.puck.owner===6&&players[6].x>=115&&players[6].y>430) {
    next={...next,cornerExitSeen:true,event:'Red exits behind net',
      message:g.mode==='guided'?'Red emerges on the far side. RD has the puck side; stay with Red C near the net.'
        :'Red carries behind the net and emerges on the far side.'}
  }
  const carrier=redCarrier(next,players)
  const redCenterReadVisible=carrier&&(carrier.id!==6||carrier.x>=185&&carrier.y>430)
  if(next.partnerSet&&redCenterReadVisible&&!next.decisions.some(d=>d.number===2)&&elapsed<DRAFT_READ_RULES.secondReadAt)
    next=sampleCoverage(next,players)
  if(next.partnerSet&&!next.decisions.some(d=>d.number===2)&&elapsed>=DRAFT_READ_RULES.secondReadAt) {
    const {eligible,covered}=next.coverage.second
    next=recordRead(next,2,eligible?covered/eligible>=DRAFT_READ_RULES.redCenter.minCoverage?'met':'missed':'unobserved',
      elapsed,eligible?'Red C coverage sampled near the blue net.':'No red puck carrier during the second read.')
  }
  const redAttacking=redCarrier(g,g.players)||g.puck.flight?.kind==='pass'&&g.players[g.puck.flight.receiver]?.team===1
  next=advancePuck(next,players,elapsed)
  if(next.endAt!=null&&g.endAt==null)
    next={...next,outcome:next.event==='Goal'?next.message:
      ({Save:'Goalie covered the puck','Zone exit':'Blue cleared the zone',Breakout:'Blue broke out'})[next.event]??next.event}
  if(redAttacking&&next.puck.owner!=null&&players[next.puck.owner]?.team===0&&players[next.puck.owner]?.slot!==5)
    next=supersedeOpenReads(next,elapsed)
  if((next.endAt!=null&&elapsed>=next.endAt)||elapsed>=SHIFT_LENGTH)
    next={...completeReads(next,elapsed),running:false,pause:'ended',event:'Shift complete',
      message:'Shift complete. Review the two draft reads and try again.'}
  const puckMoment=next.event!==g.event&&['Red looks to the slot','Red feeds the slot','Red cycles high','Pass received',
    'Pass denied','Pass intercepted','Red shot','Save','Goal'].includes(next.event)
  if(Math.round(elapsed*10)%5===0||!next.running||puckMoment)next=recordFrame(next)
  return next
}
export function act(g,action) {
  if(!g.running)return g
  const player=g.players[g.selected],owner=g.players[g.puck.owner]
  if(action==='tie') {
    const coverage=g.players[9]
    if(owner?.team===0)return{...g,event:'Blue controls',message:'Pass or clear before tying up Red C.'}
    if(g.tieUp)return{...g,event:'Tie-up active',message:'Keep your stick on Red C and hold the inside lane.'}
    if(g.elapsed<g.tieUpReadyAt)return{...g,event:'Tie-up recovering',message:'Wait a moment before tying up again.'}
    if(g.puck.owner===coverage.id)return{...g,event:'Coverage has puck',message:'Use Stick to challenge Red C with the puck.'}
    if(distance(player,coverage)>TIE_UP_REACH)
      return{...g,event:'Tie-up out of reach',message:'Move within stick reach of Red C to tie up coverage.'}
    const until=+(g.elapsed+TIE_UP_DURATION).toFixed(1)
    return{...g,tieUp:{opponent:coverage.id,until},tieUpReadyAt:+(g.elapsed+TIE_UP_COOLDOWN).toFixed(1),
      tieUps:[...g.tieUps,{at:g.elapsed,opponent:coverage.id}],event:'Coverage tied up',
      message:'You tied up Red C’s stick and blocked the net-front reception lane.'}
  }
  if(action==='poke') {
    const loose=!owner&&!g.puck.flight&&g.endAt==null&&distance(player,g.puck)<35
    if(loose||owner?.team===1&&distance(player,owner)<70)
      return{...supersedeOpenReads(g,g.elapsed),tieUp:null,slotPassAt:null,puck:{x:player.x,y:player.y,owner:player.id,flight:null},
        event:'Turnover',message:'You won the puck. Pass or clear it.'}
    return{...g,event:'Stick',message:'The carrier is outside stick reach.'}
  }
  if(g.puck.owner!==player.id)return{...g,event:'No puck',message:'Control the puck before passing or clearing.'}
  if(action==='pass')return{...g,puck:{...g.puck,owner:null,flight:{kind:'pass',receiver:player.y<397?0:1}},
    event:'Blue outlet',message:'Pass moving to the winger on your side.'}
  if(action==='shoot')return{...g,puck:{x:player.x,y:player.y,owner:null,flight:{kind:'clear',x:490,y:player.y<397?175:620}},
    event:'Blue clear',message:'Puck moving along the boards toward the neutral zone.'}
  return g
}
