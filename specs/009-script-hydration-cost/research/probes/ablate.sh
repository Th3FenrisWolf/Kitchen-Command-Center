P=$1; T=$2; shift 2
run() { label=$1; shift; env STEPS=${STEPS:-1000} "$@" node trace.mjs $P harness-notrace $T-$label > /dev/null 2>&1; node -e "const r=require('./traces/$T-$label.json');const e=r.events.filter(x=>x.dur>16);const w=Math.max(0,...e.map(x=>x.dur));const f=r.frames||[];const lf=f.filter(x=>x>50);const se=r.scrollEventsEnd||1e12;const ws=Math.max(0,...e.filter(x=>x.start<se).map(x=>x.dur));const wh=Math.max(0,...e.filter(x=>x.start>=se).map(x=>x.dur));console.log('$label'.padEnd(10),'worstEvt',w,'scroll',ws,'hover',wh,'frames',f.length,'within16.7',(100*f.filter(x=>x<=16.7).length/f.length).toFixed(1),'long',lf.length,'worstFrame',Math.round(Math.max(0,...f)), r.cards??'')"; }
for spec in "$@"; do
  case $spec in
    base) run base ;;
    nowash) run nowash CSS='.kcc-washes{display:none!important}' ;;
    nomask) run nomask CSS='.kcc-wash{mask-image:none!important;-webkit-mask-image:none!important}' ;;
    noblend) run noblend CSS='.kcc-washes{mix-blend-mode:normal!important}' ;;
    nofall) run nofall CSS='.kcc-fall{filter:none!important}' ;;
    notrans) run notrans CSS='*,*::before,*::after{transition:none!important}' ;;
    reduced) run reduced MOTION=reduced ;;
    metal) run metal GPU=metal ;;
  esac
done
