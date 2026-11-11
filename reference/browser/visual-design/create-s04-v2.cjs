const fs=require('fs'),path=require('path');
const dir=__dirname;
const source=fs.readFileSync(path.join(dir,'rink-base-clean.svg'),'utf8');
const rink=source.replace(/<svg[^>]*>/,'').replace('</svg>','');
const states=[['p1-read',1,'read'],['p1-selected',1,'selected'],['p1-feedback',1,'feedback'],['p2-read',2,'read'],['p2-selected',2,'selected'],['p2-feedback',2,'feedback'],['guided-retry',1,'retry'],['fresh-challenge',1,'fresh'],['fresh-handoff',2,'fresh']];
function frame(w,phase,state,id){
const h=1280;let s='';const esc=t=>t.replaceAll('&','&amp;').replaceAll('<','&lt;');
const text=(x,y,t,size=16,fill='#edf6ff',weight=400)=>s+=`<text x="${x}" y="${y}" font-family="Segoe UI,Arial,sans-serif" font-size="${size}" fill="${fill}" font-weight="${weight}">${esc(t)}</text>`;
const box=(x,y,ww,hh,fill,stroke='none',r=14)=>s+=`<rect x="${x}" y="${y}" width="${ww}" height="${hh}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`;
const lines=(x,y,t,size=16,color='#c9d8ec',weight=400,max=Math.floor((w-48)/(size*.54)))=>{let row='',a=[];for(const word of t.split(' ')){if((row+' '+word).trim().length>max){a.push(row);row=word;}else row=(row+' '+word).trim();}a.push(row);a.forEach((l,i)=>text(x,y+i*(size+6),l,size,color,weight));return y+a.length*(size+6);};
const selected=state==='selected',feedback=state==='feedback',fresh=state==='fresh',retry=state==='retry';
const contextClip=`context-clip-${w}-${id}`,detailClip=`detail-clip-${w}-${id}`;
s+=`<defs><clipPath id="${contextClip}" clipPathUnits="userSpaceOnUse"><rect x="20" y="228" width="${w-40}" height="78"/></clipPath><clipPath id="${detailClip}" clipPathUnits="userSpaceOnUse"><rect x="26" y="348" width="${w-52}" height="367"/></clipPath></defs>`;
box(0,0,w,h,'#0d1c32');text(20,35,'SHIFT HAPPENS!',23,'#ffffff',800);text(20,64,'DEFENSEMAN PRACTICE',14,'#86e8f1',700);
text(20,103,fresh?'Fresh challenge':retry?'Try the read again':phase===1?'Protect net front':'Read the handoff',25,'#ffffff',700);
text(20,133,`${id+1} / 9  •  ${fresh?`Unassisted • pause ${phase}`:retry?'Guided practice':`Pause ${phase} of 2`}`,16);
box(20,151,w-40,65,'#35405a');text(32,176,'PROPOSED • NOT APPROVED',14,'#ffe0a0',700);text(32,199,"For David's coaching review",15,'#ffe9bc');
// Full rink context retains exact source orientation and geometry.
s+=`<g clip-path="url(#${contextClip})"><svg x="20" y="228" width="${w-40}" height="78" viewBox="0 70 1260 650">${rink}<rect x="17" y="90" width="406" height="614" fill="#17b8c2" fill-opacity=".22" stroke="#0d6670" stroke-width="12"/></svg></g>`;
text(20,326,'Defending LEFT • zone detail below',14,'#b9d5e7');
box(20,342,w-40,382,'#edf8fc');
s+=`<g clip-path="url(#${detailClip})"><svg x="26" y="348" width="${w-52}" height="367" viewBox="0 70 460 650">${rink}`;
// Separate tactical layer: the net opening faces right toward the ice.
s+='<path d="M99 369L67 377Q52 397 67 417L99 425Z" fill="#fbe4e6" stroke="#9b1730" stroke-width="5"/><path d="M99 369V425" stroke="#5e1021" stroke-width="6"/><path d="M69 383L95 412M69 411L95 382M64 397H98" fill="none" stroke="#b96672" stroke-width="2"/><text x="104" y="450" font-family="Arial,sans-serif" font-size="23" font-weight="700" fill="#7b1c2e">NET</text>';
if(feedback||retry){s+='<ellipse cx="185" cy="397" rx="64" ry="98" fill="#13b3bb" fill-opacity=".18" stroke="#126574" stroke-width="4" stroke-dasharray="10 6"/>';}
const actor=(x,y,label,kind)=>{const controlled=kind==='you';if(controlled)s+=`<circle cx="${x}" cy="${y}" r="40" fill="none" stroke="#072c4a" stroke-width="4" stroke-dasharray="7 5"/>`;s+=kind==='opponent'?`<rect x="${x-27}" y="${y-27}" width="54" height="54" rx="7" fill="#ac3252" stroke="#711a30" stroke-width="3"/>`:`<circle cx="${x}" cy="${y}" r="29" fill="#123d59"/>`;s+=`<text x="${x}" y="${y+9}" text-anchor="middle" font-family="Arial" font-size="25" font-weight="700" fill="white">${label}</text>`;};
const flip=fresh;const oy=flip?190:595;actor(325,oy,'O1','opponent');actor(255,flip?510:255,'O2','opponent');
s+=`<circle cx="365" cy="${oy+24}" r="12" fill="#111" stroke="white" stroke-width="4"/>`;
actor(feedback&&phase===2?278:190,feedback&&phase===2?510:365,'Y','you');actor(phase===1?370:175,phase===1?430:460,'P','partner');
if(phase===1&&!fresh)s+='<path d="M355 475Q280 540 215 480" fill="none" stroke="#123d59" stroke-width="4" stroke-dasharray="10 7"/><path d="M218 500L215 480L236 482" fill="none" stroke="#123d59" stroke-width="4"/>';
s+='</svg></g>';
text(20,750,'Y = you (ring)   P = partner',15,'#e5effb',600);text(20,776,'O = opponent (square)   • = puck',15,'#c9d8ec');
let y=810;
if(feedback){y=lines(20,y,phase===1?'Proposed feedback: stay between the net and the inside threat while P returns.':'Proposed feedback: once P is set, Y takes containment. P protects net front.',17,'#ffffff',600);y=lines(20,y+10,'Shaded area = proposed protected space. David must validate these positions and the accepted answer.',16);box(20,1040,w-40,56,'#86e8f1');text(34,1075,phase===1?'Continue to pause 2':'Review both decisions',17,'#0b273a',700);lines(20,1130,phase===1?'The next pause requires a new choice. Feedback animation earns no credit.':'Exchange mastery requires both choices correct in a fresh unassisted variation.',16);}
else{
y=lines(20,y,fresh?(phase===1?'New puck side. Read the scene without a hint.':'Read the changed positions. Make your second choice.'):retry?'Hint: check P before leaving net front.':phase===1?'P is still returning. What is your next job?':'P is now set near the net. What is your next job?',17,'#ffffff',600);
const opts=phase===1?['Hold net-front coverage','Take containment now']:['Take containment','Keep net-front coverage'];
opts.forEach((t,i)=>{let yy=890+i*78;box(20,yy,w-40,66,selected&&i===0?'#17485e':'#192c46',selected&&i===0?'#86e8f1':'#61748d');text(33,yy+24,selected&&i===0?'SELECTED':'OPTION '+(i+1),12,selected&&i===0?'#86e8f1':'#b8c9e0',700);text(33,yy+49,t,17,'white',600);});
box(20,1058,w-40,56,selected?'#86e8f1':'#30415a');text(34,1093,selected?'Confirm choice':'Select an answer first',17,selected?'#0b273a':'#cbd9e9',700);
lines(20,1150,fresh?'Both handoff choices count toward exchange mastery. Five tokens available once per skill.':retry?'Guided retries are free. Earned progress stays. This attempt is not mastery evidence.':selected?'Selection stays visible until Confirm. This review screen is not scored.':'No answer is preselected. Confirm stays disabled until you choose.',16);
}
text(20,1254,'VISUAL DESIGN • No live controls',13,'#9db2ca',600);
return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" role="img" aria-label="Proposed S04 ${state} pause ${phase} for David coaching review">${s}</svg>`;
}
for(const w of [320,390]){const frames=states.map(([name,p,state],i)=>{const svg=frame(w,p,state,i);fs.writeFileSync(path.join(dir,`s04-v2-${w}-${name}.svg`),svg);return svg;});const sw=w*4+100,sh=3980;let sheet=`<svg xmlns="http://www.w3.org/2000/svg" width="${sw}" height="${sh}" viewBox="0 0 ${sw} ${sh}"><rect width="100%" height="100%" fill="#dde7ef"/>`;frames.forEach((f,i)=>sheet+=`<g transform="translate(${20+(i%4)*(w+20)},${20+Math.floor(i/4)*1320})">${f}</g>`);sheet+='</svg>';fs.writeFileSync(path.join(dir,`s04-v2-${w}-review-sheet.svg`),sheet);}
console.log('Created 18 proposed phone frames and two review sheets.');

