from pathlib import Path
from math import atan2, cos, sin, pi
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, white
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
import pypdfium2 as pdfium
from pypdf import PdfReader

out = Path('C:/Users/degra/Documents/Shift Happens/docs')
out.mkdir(exist_ok=True)
pdf = out / 'first-defensive-shift-draft.pdf'
c = canvas.Canvas(str(pdf), pagesize=(612, 792))
c.setTitle('First 12U defensive-zone shift - Draft for David')
c.setAuthor('Shift Happens!')
ink = HexColor('#163248')
blue = HexColor('#196b9b')
red = HexColor('#b74443')
green = HexColor('#28735b')

def text(x, y, value, size=9, bold=False, color=ink):
    c.setFillColor(color)
    c.setFont('Helvetica-Bold' if bold else 'Helvetica', size)
    c.drawString(x, y, value)

def para(x, top, width, value, size=9, leading=12):
    style = ParagraphStyle('body', fontName='Helvetica', fontSize=size,
                           leading=leading, textColor=ink)
    p = Paragraph(value, style)
    _, h = p.wrap(width, 700)
    p.drawOn(c, x, top-h)
    return top-h

def arrow(x1,y1,x2,y2,color=blue,dashed=False):
    c.setStrokeColor(color)
    c.setLineWidth(4)
    c.setDash(10,7) if dashed else c.setDash()
    c.line(x1,y1,x2,y2)
    c.setDash()
    angle=atan2(y2-y1,x2-x1)
    for a in (angle+pi-.45, angle+pi+.45):
        c.line(x2,y2,x2+17*cos(a),y2+17*sin(a))

def marker(x,y,label,opponent=False,controlled=False):
    c.setFillColor(red if opponent else blue)
    c.setStrokeColor(ink if controlled else white)
    c.setLineWidth(6 if controlled else 3)
    if opponent:
        c.rect(x-23,y-18,46,36,fill=1,stroke=1)
    else:
        c.circle(x,y,23,fill=1,stroke=1)
    c.setFillColor(white)
    c.setFont('Helvetica-Bold',24)
    c.saveState()
    c.translate(x,y)
    c.scale(1,-1)
    c.drawCentredString(0,-8,label)
    c.restoreState()

def rink(x, top, phase):
    c.saveState()
    # Source-rink left-zone orientation and schematic markings retained.
    c.translate(x, top)
    c.scale(.305,-.305)
    c.translate(-10,-80)
    c.setFillColor(HexColor('#f4f9fc'))
    c.setStrokeColor(HexColor('#7993a3'))
    c.setLineWidth(4)
    p=c.beginPath()
    p.moveTo(423,90); p.lineTo(160,90)
    p.curveTo(80,90,17,154,17,234); p.lineTo(17,559)
    p.curveTo(17,639,80,704,160,704); p.lineTo(423,704)
    p.close()
    c.drawPath(p,fill=1,stroke=1)
    c.setStrokeColor(blue); c.line(423,90,423,704)
    c.setStrokeColor(HexColor('#d8a4a3'));c.setLineWidth(2)
    c.line(99,104,99,690)
    for yy in (244,550):
        c.circle(242,yy,90,stroke=1,fill=0)
        c.circle(242,yy,5,stroke=1,fill=0)
    c.setFillColor(HexColor('#d2e9f6'))
    p=c.beginPath();p.moveTo(99,360);p.curveTo(148,360,148,434,99,434);p.close()
    c.drawPath(p,fill=1,stroke=1)
    c.setStrokeColor(red); c.rect(74,373,25,48,fill=0,stroke=1)
    # Blue circles and red squares identify teams independently of color.
    if phase==0:
        arrow(115,620,170,417,blue,True)
        arrow(173,181,210,371,red,True)
    elif phase==1:
        arrow(169,338,177,248,green)
    else:
        arrow(170,290,176,232,green)
        arrow(174,190,365,163,red,True)
    positions = [(183,190,'RW'),(210,375,'C'),(260,500,'LW'),
                 (390,152,'D'),(390,635,'D')]
    for xx,yy,label in positions: marker(xx,yy,label,True)
    marker(122,398,'G')
    marker(330,245,'LW');marker(330,550,'RW');marker(225,465,'C')
    if phase==0:
        marker(169,343,'Y',controlled=True);marker(115,620,'RD')
    else:
        marker(170,417,'RD')
        marker(169,338 if phase==1 else 285,'Y',controlled=True)
    c.setFillColor(ink);c.circle(199,163,8,fill=1,stroke=0)
    c.restoreState()

text(30,758,'SHIFT HAPPENS!  /  FIRST 12U DEFENSIVE SHIFT',10,True)
text(30,733,'Hold the inside. Read the handoff. Contain.',19,True)
text(30,713,'DRAFT FOR DAVID\'S REVIEW  |  November 10, 2026',9,True,red)
para(30,699,552,'<b>Proposed same-side return variation.</b> Red RW keeps controlled possession low on LD\'s side while Blue RD returns from the bottom corner. One controlled LD (Y), local single-player, elevated view. Suggested shift cap is 30 seconds.',9,12)

for i,(title,caption) in enumerate([
    ('1  HOLD NET FRONT','Y protects Red C\'s inside lane.<br/>RD returns from the bottom corner.'),
    ('2  CONFIRM THE HANDOFF','RD takes Red C with an inside stick.<br/>Y scans puck, partner and slot.'),
    ('3  MOVE TO CONTAIN','Y closes inside-out under control.<br/>RD keeps Red C. Green = Y\'s route.')]):
    x=30+i*187
    text(x,651,title,10,True)
    rink(x+19,640,i)
    para(x,439,174,caption,8.5,11)
para(30,407,552,'<b>Diagram key</b> Blue circles defend the left net. Red squares attack. Y = controlled LD. Black dot = puck. Dashed blue = RD return. Dashed red = pass option. Positions are schematic. All five skaters per team are shown. Red goalie is off-frame at the far end.',8,10)

text(30,369,'STARTING RESPONSIBILITIES',10,True)
para(30,359,552,'<b>Y / LD</b> Stay goal-side of Red C and protect the direct feed. <b>RD</b> Return from the bottom corner along the inside to take net-front coverage. <b>Blue C</b> Track Red LW in the low slot. <b>LW</b> Protect the puck-side point lane. <b>RW</b> Support the weak-side high slot. <b>G</b> Track puck and protect the crease. Red C threatens the slot, LW offers a weak-side feed, and both D offer point outlets.',8.7,11.4)

text(30,296,'DECISION 1  /  IS COVERAGE ESTABLISHED?',9.5,True)
para(30,285,267,'<b>Observe</b> puck control, Red C\'s stick, RD\'s route and Blue C\'s assignment. Hold until RD is goal-side of Red C, can deny the direct feed, sees puck and threat, and can stay with a cut while Y leaves. A post arrival or timer alone is insufficient.<br/><b>Cue</b> "Partner still returning. Protect the middle." Leaving early can open a direct feed or rebound lane.',8.7,11.5)
text(318,296,'DECISION 2  /  CAN Y CONTAIN NOW?',9.5,True)
para(318,285,264,'<b>Act</b> once RD has taken Red C and the carrier remains on Y\'s side. Close inside-out, keep the middle pass in reach, and match speed to contain. Recheck coverage and recover inside if it breaks down.<br/><b>Cue</b> "RD has the middle. Close from inside." A contested touch keeps possession uncertain. If RD goes to the carrier, Y keeps net front. David must validate this assignment.',8.7,11.5)

text(30,172,'REPEATABLE TESTS FOR BOTH DECISIONS',10,True)
para(30,161,552,'<b>Paired runs</b> Replay the same seeded setup with (1) Y holding versus leaving before RD takes over and (2) timely containment versus a late close or an inside-lane overcommit. Repeat with RD delayed, wrong-side or drawn away, plus a contested puck. Mirror the rink and vary carrier speed and receiver cuts.<br/><b>Measure</b> uncovered-lane time, goal-side responsibility, feasible stick reach, close timing, carrier options and recovery. Record positions, facing, ownership, pass paths and results. Judge the observed action separately from a goal or save. Mark a read unobserved when turnover or whistle removes its opportunity.',8.7,11.5)
para(30,86,552,'<b>Playback checks</b> Guided uses one pre-handoff pause and a brief live cue. Freer uses the same simulation without automatic pauses. Pass, intercept, loose puck, shot, pause/resume, restart and replay must match recorded state. Feedback must match the observed cue, action and consequence. Attacking intent may be scripted. Lanes and outcomes must respond to play. Verify desktop standalone and phone separately.',8.1,10.5)
para(30,40,552,'<b>Review basis</b> Local Box + 1 notes and supplied rink. Video linked in those notes was unavailable for re-review. Existing behind-net exchange assigns RD containment and Y net front. This proposed same-side variation needs David\'s approval of roles, handoff geometry, thresholds and feedback. Incorporate Brian\'s feedback when available.',7.3,9)
c.linkURL('file:///C:/Users/degra/OneDrive/Documents/ChatGPT/Youth%20Hockey%20Coach/Shift%20Happens!/BOX-PLUS-ONE-MODEL.md',(86,30,160,40),relative=0)
c.save()
reader=PdfReader(str(pdf))
assert len(reader.pages)==1
page_text=reader.pages[0].extract_text()
for required in ['DRAFT FOR DAVID','DECISION 1','DECISION 2','REPEATABLE TESTS','same-side','unobserved']:
    assert required in page_text, required
document=pdfium.PdfDocument(str(pdf))
document[0].render(scale=2).to_pil().save(out/'first-defensive-shift-draft.png')
print(f'Created one-page PDF and preview in {out}')
print(f'Extracted {len(page_text.split())} words')
