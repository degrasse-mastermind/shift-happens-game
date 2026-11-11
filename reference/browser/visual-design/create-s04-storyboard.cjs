const fs=require('fs'),path=require('path');const dir=__dirname;
const rink=fs.readFileSync(path.join(dir,'rink-base-clean.svg'),'utf8').replace(/<svg[^>]*>/,'').replace('</svg>','');
const esc=s=>s.replaceAll('&','&amp;');
function frame(w,phase){
let h=910,s='';const text=(x,y,t,size=15,fill='#eff6ff',weight=400)=>s+=`<text x="${x}" y="${y}" font-family="Segoe UI,Arial,sans-serif" font-size="${size}" fill="${fill}" font-weight="${weight}">${esc(t)}</text>`;
const box=(x,y,width,height,fill,stroke='none',r=14)=>s+=`<rect x="${x}" y="${y}" width="${width}" height="${height}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`;
box(0,0,w,h,'#101c31');text(20,34,'SHIFT HAPPENS!',22,'#ffffff',800);text(20,61,'DEFENSEMAN PRACTICE',12,'#7eddeb',700);
text(20,100,phase===1?'Protect net front':'Partner set. Your next read.',w===320?21:24,'#ffffff',700);
text(20,128,phase===1?'Pause 1 of 2 · Partner returning':'Pause 2 of 2 · Make a new choice',14,'#b9c9df');
box(20,148,w-40,40,'#293851');text(32,174,'DRAFT · Coach review required',13,'#fff0b4',600);
box(20,204,w-40,270,'#eaf6fc');
s+=`<svg x="28" y="212" width="${w-56}" height="242" viewBox="0 80 460 630" overflow="hidden">${rink}`;
// Actors are deliberately separate from the unchanged source rink.
s+='<g font-family="Segoe UI,Arial" font-size="18" font-weight="700" text-anchor="middle">';
const actor=(x,y,label,controlled=false)=>{s+=`<circle cx="${x}" cy="${y}" r="${controlled?30:23}" fill="#123750" stroke="#123750" stroke-width="3"/>`;if(controlled)s+=`<circle cx="${x}" cy="${y}" r="37" fill="none" stroke="#123750" stroke-width="4" stroke-dasharray="8 4"/>`;s+=`<text x="${x}" y="${y+6}" fill="white">${label}</text>`;};
actor(175,395,'YOU',true);actor(phase===1?330:130,phase===1?580:475,'P');s+='</g></svg>';
text(32,462,'Defending zone · P = partner',12,'#213f57');
text(20,502,phase===1?'Your partner is still returning.':'Your partner is visibly set.',15,'#d4e0ef',600);
text(20,526,phase===1?'Choose your next responsibility.':'Choose your next responsibility.',14,'#b9c9df');
const choices=phase===1?['Hold net-front coverage','Take containment now']:['Take the containment job','Keep the same job'];
choices.forEach((c,i)=>{box(20,548+i*66,w-40,54,i===0?'#174c60':'#1b2b43',i===0?'#85edf1':'#53657e');text(34,581+i*66,(i===0?'[Selected] ':'[ ] ')+c,w===320?12:14,'#ffffff',i===0?700:400);});
box(20,690,w-40,52,'#85edf1');text(w/2-55,723,'Confirm choice',16,'#092735',700);
text(20,772,'SELECTED STATE · Not scored',12,'#85edf1',700);
text(20,799,phase===1?'Then: explain the protected space.':'Then: review the full exchange.',14);
text(20,823,'Guided retry: free. Fresh variation:',14);text(20,847,'unassisted mastery check.',14);
text(20,884,'Visual storyboard only · No live controls',11,'#b9c9df');
return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" role="img" aria-label="S04 pause ${phase}, ${w} pixel design storyboard">${s}</svg>`;
}
for(const w of [390,320])for(const p of [1,2]){let name=`s04-storyboard-${w}-pause-${p}.svg`;fs.writeFileSync(path.join(dir,name),frame(w,p));}
console.log('Created four SVG storyboard frames. Original rink unchanged.');

