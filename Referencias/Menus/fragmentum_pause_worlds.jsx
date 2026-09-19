import { useState, useEffect } from "react";

// ═══════════════════════════════════════════════════════════════
// SCENE COMPONENTS — uno por cada mundo emocional
// ═══════════════════════════════════════════════════════════════

// I. IRA — Fortaleza Volcánica
function IraScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <linearGradient id="iraSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#1a0810" />
          <stop offset="35%" stopColor="#3d1020" />
          <stop offset="70%" stopColor="#6a1818" />
          <stop offset="100%" stopColor="#2a0608" />
        </linearGradient>
        <radialGradient id="iraSun" cx="60%" cy="55%" r="40%">
          <stop offset="0%" stopColor="#ff8c42" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#ff6b35" stopOpacity="0" />
        </radialGradient>
        <linearGradient id="iraGround" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#1a0408" />
          <stop offset="100%" stopColor="#000" />
        </linearGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#iraSky)" />
      <rect width="1600" height="900" fill="url(#iraSun)" />
      <path d="M0,500 L120,440 L240,460 L360,400 L490,440 L620,420 L760,450 L900,400 L1040,430 L1180,410 L1320,440 L1460,420 L1600,440 L1600,900 L0,900 Z" fill="#1a0410" opacity="0.85" />
      <path d="M0,610 L180,540 L340,570 L500,500 L660,540 L820,520 L980,560 L1140,510 L1300,540 L1460,520 L1600,540 L1600,900 L0,900 Z" fill="#0a0204" opacity="0.95" />
      {/* Volcanic tower with Centinela Ardiente */}
      <g>
        <rect x="1280" y="360" width="70" height="340" fill="#040206" />
        <rect x="1265" y="350" width="100" height="14" fill="#040206" />
        <polygon points="1280,360 1315,310 1350,360" fill="#040206" />
        <circle cx="1315" cy="385" r="6" fill="#ff4c00">
          <animate attributeName="opacity" values="0.4;1;0.4" dur="1.8s" repeatCount="indefinite" />
        </circle>
        <rect x="1295" y="450" width="6" height="10" fill="#ff4c4c" opacity="0.6" />
        <rect x="1330" y="500" width="6" height="10" fill="#ff4c4c" opacity="0.6" />
      </g>
      <rect y="450" width="1600" height="250" fill="#000" opacity="0.18" />
      {/* Platforms */}
      <g fill="#0a0408">
        <rect x="180" y="660" width="240" height="22" /><rect x="490" y="590" width="200" height="22" />
        <rect x="760" y="640" width="220" height="22" /><rect x="1040" y="600" width="180" height="22" />
      </g>
      <rect y="710" width="1600" height="190" fill="url(#iraGround)" />
      {/* Lava cracks */}
      <g stroke="#ff4c00" strokeWidth="1.5" fill="none" opacity="0.6">
        <path d="M200,720 L210,760 L195,820 L220,870" />
        <path d="M850,720 L860,790 L840,850" />
        <path d="M1200,720 L1190,810 L1210,870" />
      </g>
      {/* Embers */}
      {Array.from({ length: 18 }).map((_, i) => (
        <circle key={i} cx={60 + i * 88} cy={900} r={1.5 + (i % 3) * 0.8}
          fill={i % 3 === 0 ? "#ff8c42" : i % 3 === 1 ? "#ff4c4c" : "#ffa55c"}
          style={{ animation: `ember ${4 + (i % 5)}s ease-out ${i * 0.3}s infinite`, filter: "drop-shadow(0 0 4px currentColor)" }} />
      ))}
      {/* Kael */}
      <g transform="translate(595, 542)">
        <ellipse cx="0" cy="46" rx="18" ry="3" fill="#6B3FA0" opacity="0.35" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#08040a" />
        <rect x="-9" y="5" width="18" height="32" fill="#0a050d" />
        <rect x="-7" y="-12" width="14" height="14" fill="#0a050d" />
        <rect x="14" y="0" width="22" height="3" fill="#1a1a2a" />
        <circle cx="-2" cy="-7" r="0.8" fill="#B06EFF" />
        <circle cx="3" cy="-7" r="0.8" fill="#B06EFF" />
      </g>
      {/* Centinela Ardiente */}
      <g transform="translate(1090, 590)" style={{ animation: "float 3.5s ease-in-out infinite" }}>
        <ellipse cx="0" cy="0" rx="40" ry="50" fill="#ff4c00" opacity="0.12" />
        <path d="M-26,-10 L-32,38 L32,38 L26,-10 Z" fill="#100200" />
        <ellipse cx="0" cy="-20" rx="20" ry="26" fill="#100200" />
        <circle cx="-7" cy="-22" r="2.5" fill="#ff4c00"><animate attributeName="opacity" values="0.5;1;0.5" dur="2.2s" repeatCount="indefinite" /></circle>
        <circle cx="7" cy="-22" r="2.5" fill="#ff4c00"><animate attributeName="opacity" values="0.5;1;0.5" dur="2.2s" repeatCount="indefinite" /></circle>
        <ellipse cx="0" cy="42" rx="22" ry="4" fill="#ff4c00" opacity="0.3" />
      </g>
    </svg>
  );
}

// II. TRISTEZA — Ciudad Sumergida
function TristezaScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <linearGradient id="trisSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#020a18" />
          <stop offset="40%" stopColor="#051830" />
          <stop offset="80%" stopColor="#031020" />
          <stop offset="100%" stopColor="#01040c" />
        </linearGradient>
        <radialGradient id="trisLight" cx="50%" cy="0%" r="60%">
          <stop offset="0%" stopColor="#00d4ff" stopOpacity="0.18" />
          <stop offset="100%" stopColor="#00d4ff" stopOpacity="0" />
        </radialGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#trisSky)" />
      <rect width="1600" height="900" fill="url(#trisLight)" />
      {/* Light rays from above */}
      <g opacity="0.15">
        <polygon points="400,0 350,900 450,900" fill="#00d4ff" />
        <polygon points="900,0 870,900 930,900" fill="#00d4ff" />
        <polygon points="1300,0 1270,900 1330,900" fill="#00d4ff" />
      </g>
      {/* Submerged buildings */}
      <g fill="#020812" opacity="0.92">
        <rect x="100" y="450" width="80" height="280" />
        <rect x="120" y="430" width="40" height="25" />
        <rect x="300" y="400" width="120" height="350" />
        <rect x="320" y="380" width="80" height="25" />
        <polygon points="320,400 360,330 400,400" />
        <rect x="600" y="500" width="60" height="220" />
        <rect x="1000" y="380" width="100" height="380" />
        <polygon points="1000,380 1050,310 1100,380" />
        <rect x="1300" y="450" width="90" height="290" />
      </g>
      {/* Window lights */}
      <g fill="#00d4ff" opacity="0.4">
        <rect x="135" y="480" width="6" height="6" /><rect x="145" y="520" width="6" height="6" />
        <rect x="340" y="450" width="6" height="6" /><rect x="380" y="480" width="6" height="6" />
        <rect x="1030" y="450" width="6" height="6" /><rect x="1070" y="500" width="6" height="6" />
      </g>
      <rect y="450" width="1600" height="450" fill="#020618" opacity="0.4" />
      {/* Platforms */}
      <g fill="#051022">
        <rect x="500" y="610" width="220" height="22" /><rect x="780" y="570" width="180" height="22" />
      </g>
      <rect y="710" width="1600" height="190" fill="#000" />
      <rect y="710" width="1600" height="2" fill="#0a2840" opacity="0.6" />
      {/* Bubbles rising */}
      {Array.from({ length: 14 }).map((_, i) => (
        <circle key={i} cx={80 + i * 110} cy={900} r={3 + (i % 4)}
          fill="#00d4ff" opacity="0.5" stroke="#fff" strokeWidth="0.5" strokeOpacity="0.3"
          style={{ animation: `bubble ${5 + (i % 4)}s ease-out ${i * 0.4}s infinite` }} />
      ))}
      {/* Rain */}
      {Array.from({ length: 18 }).map((_, i) => (
        <line key={`r${i}`} x1={50 + i * 90} y1={-20} x2={50 + i * 90} y2={5} stroke="#00d4ff" strokeWidth="1" opacity="0.4"
          style={{ animation: `rain ${1.5 + (i % 3) * 0.4}s linear ${i * 0.2}s infinite` }} />
      ))}
      {/* Kael */}
      <g transform="translate(595, 562)">
        <ellipse cx="0" cy="46" rx="18" ry="3" fill="#00d4ff" opacity="0.35" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#02060e" />
        <rect x="-9" y="5" width="18" height="32" fill="#03081a" />
        <rect x="-7" y="-12" width="14" height="14" fill="#03081a" />
        <rect x="14" y="0" width="22" height="3" fill="#0a1828" />
        <circle cx="-2" cy="-7" r="0.8" fill="#00d4ff" />
        <circle cx="3" cy="-7" r="0.8" fill="#00d4ff" />
      </g>
      {/* Doncella del Lamento */}
      <g transform="translate(1090, 600)" style={{ animation: "float 4s ease-in-out infinite" }}>
        <ellipse cx="0" cy="0" rx="40" ry="60" fill="#00d4ff" opacity="0.08" />
        {/* Long flowing dress */}
        <path d="M-20,-20 L-30,60 L30,60 L20,-20 Z" fill="#03081a" />
        <ellipse cx="0" cy="-25" rx="16" ry="22" fill="#03081a" />
        {/* Veil teardrops */}
        <line x1="-4" y1="-20" x2="-4" y2="-10" stroke="#00d4ff" strokeWidth="1.5" opacity="0.8" />
        <line x1="4" y1="-20" x2="4" y2="-10" stroke="#00d4ff" strokeWidth="1.5" opacity="0.8" />
        <ellipse cx="0" cy="63" rx="24" ry="5" fill="#00d4ff" opacity="0.2" />
      </g>
    </svg>
  );
}

// III. MIEDO — Bosque Tenebroso
function MiedoScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <linearGradient id="miedoSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#040a06" />
          <stop offset="50%" stopColor="#080f0a" />
          <stop offset="100%" stopColor="#020403" />
        </linearGradient>
        <radialGradient id="miedoFog" cx="50%" cy="80%" r="60%">
          <stop offset="0%" stopColor="#1a3a28" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#000" stopOpacity="0" />
        </radialGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#miedoSky)" />
      <rect width="1600" height="900" fill="url(#miedoFog)" />
      {/* Twisted trees — far layer */}
      <g fill="#020604" opacity="0.85">
        {[150, 280, 420, 590, 740, 900, 1080, 1230, 1380].map((x, i) => (
          <g key={i} transform={`translate(${x},0)`}>
            <path d={`M0,700 Q${-5 + (i % 3)},500 ${(i % 5) * 2},400 Q${3 + (i % 4)},300 ${(i % 3) * 3},220`} stroke="#020604" strokeWidth={6 + (i % 3)} fill="none" />
            <path d={`M${(i % 3) * 3},220 L${-15 + i * 2},150`} stroke="#020604" strokeWidth="3" />
            <path d={`M${(i % 3) * 3},220 L${20 - i},180`} stroke="#020604" strokeWidth="3" />
            <path d={`M${(i % 3) * 3},220 L${5 + i},120`} stroke="#020604" strokeWidth="2" />
          </g>
        ))}
      </g>
      {/* Mid trees larger */}
      <g fill="#010301" opacity="0.95">
        {[80, 350, 670, 1000, 1320, 1520].map((x, i) => (
          <g key={i} transform={`translate(${x},0)`}>
            <path d={`M0,750 Q${10 + i},550 ${5 - i},400 Q${-8 + i},250 ${10 + i},150`} stroke="#010301" strokeWidth="14" fill="none" />
            <path d={`M${10 + i},150 L${-30},80`} stroke="#010301" strokeWidth="5" />
            <path d={`M${10 + i},150 L${50},60`} stroke="#010301" strokeWidth="5" />
            <path d={`M${10 + i},150 L${0},20`} stroke="#010301" strokeWidth="4" />
          </g>
        ))}
      </g>
      {/* Fog layer */}
      <rect y="500" width="1600" height="400" fill="#1a3a28" opacity="0.15" />
      <rect y="650" width="1600" height="250" fill="#0a1a12" opacity="0.4" />
      {/* Platforms */}
      <g fill="#020403">
        <rect x="500" y="610" width="220" height="22" /><rect x="780" y="570" width="180" height="22" />
      </g>
      <rect y="710" width="1600" height="190" fill="#010201" />
      {/* Floating fog particles */}
      {Array.from({ length: 10 }).map((_, i) => (
        <ellipse key={i} cx={100 + i * 160} cy={650 + (i % 3) * 40} rx={40} ry={8}
          fill="#7FE0B0" opacity="0.1"
          style={{ animation: `fogDrift ${8 + (i % 4)}s ease-in-out ${i * 0.5}s infinite` }} />
      ))}
      {/* Hidden eyes in distance */}
      <g style={{ animation: "blink 4s ease-in-out infinite" }}>
        <circle cx="200" cy="450" r="2" fill="#7FE0B0" opacity="0.7" />
        <circle cx="210" cy="450" r="2" fill="#7FE0B0" opacity="0.7" />
      </g>
      <g style={{ animation: "blink 5s ease-in-out 1s infinite" }}>
        <circle cx="1380" cy="500" r="2" fill="#7FE0B0" opacity="0.7" />
        <circle cx="1390" cy="500" r="2" fill="#7FE0B0" opacity="0.7" />
      </g>
      {/* Kael */}
      <g transform="translate(595, 562)">
        <ellipse cx="0" cy="46" rx="18" ry="3" fill="#7FE0B0" opacity="0.25" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#000" />
        <rect x="-9" y="5" width="18" height="32" fill="#020403" />
        <rect x="-7" y="-12" width="14" height="14" fill="#020403" />
        <rect x="14" y="0" width="22" height="3" fill="#0a1208" />
        <circle cx="-2" cy="-7" r="0.8" fill="#7FE0B0" />
        <circle cx="3" cy="-7" r="0.8" fill="#7FE0B0" />
      </g>
      {/* Espectro del Pavor — partially hidden */}
      <g transform="translate(1090, 580)" opacity="0.7" style={{ animation: "float 5s ease-in-out infinite" }}>
        <ellipse cx="0" cy="0" rx="35" ry="55" fill="#7FE0B0" opacity="0.06" />
        <path d="M-22,-15 L-28,50 L28,50 L22,-15 Z" fill="#000" />
        <ellipse cx="0" cy="-22" rx="16" ry="24" fill="#000" />
        <circle cx="-6" cy="-24" r="3" fill="#7FE0B0">
          <animate attributeName="opacity" values="0.3;1;0.3" dur="1.8s" repeatCount="indefinite" />
        </circle>
        <circle cx="6" cy="-24" r="3" fill="#7FE0B0">
          <animate attributeName="opacity" values="0.3;1;0.3" dur="1.8s" repeatCount="indefinite" />
        </circle>
      </g>
      {/* Strong vignette for limited visibility */}
      <radialGradient id="miedoVignette" cx="50%" cy="50%" r="50%">
        <stop offset="40%" stopColor="#000" stopOpacity="0" />
        <stop offset="100%" stopColor="#000" stopOpacity="0.8" />
      </radialGradient>
      <rect width="1600" height="900" fill="url(#miedoVignette)" />
    </svg>
  );
}

// IV. CULPA — Campos de Batalla Arrasados
function CulpaScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <linearGradient id="culpaSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#1a1610" />
          <stop offset="50%" stopColor="#28201a" />
          <stop offset="100%" stopColor="#100c08" />
        </linearGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#culpaSky)" />
      {/* Distant smoke pillars */}
      <g opacity="0.4">
        <ellipse cx="300" cy="500" rx="40" ry="200" fill="#3a3028" />
        <ellipse cx="800" cy="450" rx="50" ry="250" fill="#3a3028" />
        <ellipse cx="1300" cy="480" rx="35" ry="180" fill="#3a3028" />
      </g>
      {/* Distant hills */}
      <path d="M0,600 L200,540 L350,570 L500,500 L660,540 L820,520 L980,560 L1140,510 L1300,540 L1460,520 L1600,540 L1600,900 L0,900 Z" fill="#15110a" opacity="0.95" />
      <rect y="450" width="1600" height="250" fill="#000" opacity="0.15" />
      {/* Battlefield debris */}
      <g stroke="#100a05" strokeWidth="3" fill="#100a05">
        {/* Broken swords stuck in ground */}
        <g transform="translate(280, 700) rotate(15)">
          <rect x="-2" y="-40" width="4" height="40" />
          <rect x="-8" y="-2" width="16" height="3" />
        </g>
        <g transform="translate(450, 700) rotate(-20)">
          <rect x="-2" y="-50" width="4" height="50" />
          <rect x="-10" y="-2" width="20" height="3" />
        </g>
        <g transform="translate(1100, 700) rotate(10)">
          <rect x="-2" y="-45" width="4" height="45" />
          <rect x="-9" y="-2" width="18" height="3" />
        </g>
        <g transform="translate(1350, 700) rotate(-15)">
          <rect x="-2" y="-38" width="4" height="38" />
          <rect x="-8" y="-2" width="16" height="3" />
        </g>
        {/* Broken shields/helmets */}
        <ellipse cx="200" cy="720" rx="14" ry="6" />
        <ellipse cx="950" cy="715" rx="16" ry="7" />
      </g>
      {/* Platforms */}
      <g fill="#1a140a">
        <rect x="500" y="610" width="220" height="22" /><rect x="780" y="570" width="180" height="22" />
      </g>
      <rect y="710" width="1600" height="190" fill="#0a0805" />
      <rect y="710" width="1600" height="2" fill="#3a3020" opacity="0.6" />
      {/* Falling ash */}
      {Array.from({ length: 22 }).map((_, i) => (
        <circle key={i} cx={50 + i * 75} cy={-10} r={1 + (i % 2) * 0.5}
          fill="#5a4e3a" opacity="0.7"
          style={{ animation: `ash ${6 + (i % 4)}s linear ${i * 0.3}s infinite` }} />
      ))}
      {/* Crows in distant sky */}
      <g fill="#000" opacity="0.6" style={{ animation: "float 6s ease-in-out infinite" }}>
        <path d="M400,250 Q405,245 410,250 Q415,245 420,250" stroke="#000" strokeWidth="1.5" fill="none" />
        <path d="M450,280 Q453,277 456,280 Q459,277 462,280" stroke="#000" strokeWidth="1.5" fill="none" />
        <path d="M1100,230 Q1105,225 1110,230 Q1115,225 1120,230" stroke="#000" strokeWidth="1.5" fill="none" />
      </g>
      {/* Kael */}
      <g transform="translate(595, 562)">
        <ellipse cx="0" cy="46" rx="18" ry="3" fill="#C9A567" opacity="0.3" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#08060a" />
        <rect x="-9" y="5" width="18" height="32" fill="#0a070a" />
        <rect x="-7" y="-12" width="14" height="14" fill="#0a070a" />
        <rect x="14" y="0" width="22" height="3" fill="#1a1a2a" />
        <circle cx="-2" cy="-7" r="0.8" fill="#C9A567" />
        <circle cx="3" cy="-7" r="0.8" fill="#C9A567" />
      </g>
      {/* Juez Silente — judge with scales */}
      <g transform="translate(1090, 580)" style={{ animation: "float 4.5s ease-in-out infinite" }}>
        <ellipse cx="0" cy="0" rx="40" ry="55" fill="#C9A567" opacity="0.08" />
        {/* Robed body */}
        <path d="M-28,-10 L-34,55 L34,55 L28,-10 Z" fill="#080604" />
        <ellipse cx="0" cy="-22" rx="20" ry="26" fill="#080604" />
        {/* Hood */}
        <path d="M-18,-30 L-22,-10 L22,-10 L18,-30 Z" fill="#0a0805" />
        {/* Eyes */}
        <circle cx="-7" cy="-22" r="2" fill="#C9A567">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="3s" repeatCount="indefinite" />
        </circle>
        <circle cx="7" cy="-22" r="2" fill="#C9A567">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="3s" repeatCount="indefinite" />
        </circle>
        {/* Scales floating beside */}
        <line x1="-30" y1="0" x2="-50" y2="0" stroke="#C9A567" strokeWidth="1" opacity="0.6" />
        <circle cx="-50" cy="5" r="4" fill="none" stroke="#C9A567" strokeWidth="1" opacity="0.6" />
        <line x1="30" y1="0" x2="50" y2="0" stroke="#C9A567" strokeWidth="1" opacity="0.6" />
        <circle cx="50" cy="5" r="4" fill="none" stroke="#C9A567" strokeWidth="1" opacity="0.6" />
        <ellipse cx="0" cy="58" rx="26" ry="5" fill="#C9A567" opacity="0.2" />
      </g>
    </svg>
  );
}

// V. ESPERANZA — Jardín Etéreo
function EsperanzaScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <linearGradient id="espSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#2a1f3a" />
          <stop offset="40%" stopColor="#3a2a4a" />
          <stop offset="80%" stopColor="#4a3055" />
          <stop offset="100%" stopColor="#2a1830" />
        </linearGradient>
        <radialGradient id="espGlow" cx="50%" cy="40%" r="50%">
          <stop offset="0%" stopColor="#FFE4A0" stopOpacity="0.3" />
          <stop offset="100%" stopColor="#FFE4A0" stopOpacity="0" />
        </radialGradient>
        <radialGradient id="treeGlow" cx="50%" cy="60%" r="50%">
          <stop offset="0%" stopColor="#FFE4A0" stopOpacity="0.4" />
          <stop offset="100%" stopColor="#FFE4A0" stopOpacity="0" />
        </radialGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#espSky)" />
      <rect width="1600" height="900" fill="url(#espGlow)" />
      {/* Floating islands far */}
      <g opacity="0.7">
        <ellipse cx="250" cy="400" rx="80" ry="20" fill="#2a1f3a" />
        <ellipse cx="850" cy="350" rx="100" ry="25" fill="#2a1f3a" />
        <ellipse cx="1350" cy="420" rx="70" ry="18" fill="#2a1f3a" />
      </g>
      {/* Distant ethereal trees with glow */}
      {[150, 400, 720, 1050, 1380].map((x, i) => (
        <g key={i} transform={`translate(${x}, 0)`}>
          <circle cx="0" cy="500" r="60" fill="url(#treeGlow)" />
          <path d={`M0,700 Q5,600 0,500 Q-5,400 0,300`} stroke="#1a1228" strokeWidth="8" fill="none" />
          <g fill="#FFE4A0" opacity="0.5">
            <circle cx="-15" cy="450" r="3" />
            <circle cx="20" cy="440" r="3" />
            <circle cx="-5" cy="380" r="3" />
            <circle cx="10" cy="350" r="3" />
            <circle cx="-20" cy="380" r="2" />
          </g>
        </g>
      ))}
      <rect y="450" width="1600" height="450" fill="#1a1228" opacity="0.3" />
      {/* Platforms */}
      <g fill="#1a1228">
        <rect x="500" y="610" width="220" height="22" /><rect x="780" y="570" width="180" height="22" />
      </g>
      {/* Glowing edge on platforms */}
      <g fill="#FFE4A0" opacity="0.5">
        <rect x="500" y="608" width="220" height="2" /><rect x="780" y="568" width="180" height="2" />
      </g>
      <rect y="710" width="1600" height="190" fill="#0a0518" />
      <rect y="710" width="1600" height="2" fill="#FFE4A0" opacity="0.4" />
      {/* Rising fireflies */}
      {Array.from({ length: 24 }).map((_, i) => (
        <circle key={i} cx={50 + i * 65} cy={900} r={1.5 + (i % 3) * 0.5}
          fill="#FFE4A0"
          style={{ animation: `firefly ${6 + (i % 5)}s ease-out ${i * 0.3}s infinite`, filter: "drop-shadow(0 0 6px currentColor)" }} />
      ))}
      {/* Central beautiful fragment */}
      <g transform="translate(880, 480)" style={{ animation: "float 5s ease-in-out infinite" }}>
        <circle r="40" fill="#FFE4A0" opacity="0.15">
          <animate attributeName="r" values="35;45;35" dur="3s" repeatCount="indefinite" />
        </circle>
        <polygon points="0,-18 14,0 0,18 -14,0" fill="#FFE4A0" />
        <polygon points="0,-18 14,0 0,18 -14,0" fill="none" stroke="#fff" strokeWidth="1" />
        <polygon points="0,-18 6,-6 -6,-6" fill="#fff" opacity="0.7" />
      </g>
      {/* Kael — at peace */}
      <g transform="translate(595, 562)">
        <ellipse cx="0" cy="46" rx="20" ry="3" fill="#FFE4A0" opacity="0.5" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#1a1228" />
        <rect x="-9" y="5" width="18" height="32" fill="#1a1228" />
        <rect x="-7" y="-12" width="14" height="14" fill="#1a1228" />
        <rect x="14" y="0" width="22" height="3" fill="#3a2a4a" />
        <circle cx="-2" cy="-7" r="0.8" fill="#FFE4A0" />
        <circle cx="3" cy="-7" r="0.8" fill="#FFE4A0" />
      </g>
    </svg>
  );
}

// VI. FINAL — El Espejo del Origen
function FinalScene() {
  return (
    <svg viewBox="0 0 1600 900" preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}>
      <defs>
        <radialGradient id="finalSky" cx="50%" cy="50%" r="80%">
          <stop offset="0%" stopColor="#1a0a28" />
          <stop offset="60%" stopColor="#080410" />
          <stop offset="100%" stopColor="#000" />
        </radialGradient>
        <linearGradient id="mirrorLine" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#B06EFF" stopOpacity="0" />
          <stop offset="50%" stopColor="#B06EFF" stopOpacity="0.6" />
          <stop offset="100%" stopColor="#B06EFF" stopOpacity="0" />
        </linearGradient>
      </defs>
      <rect width="1600" height="900" fill="url(#finalSky)" />
      {/* Central vertical mirror line */}
      <rect x="798" y="0" width="4" height="900" fill="url(#mirrorLine)">
        <animate attributeName="opacity" values="0.4;1;0.4" dur="3s" repeatCount="indefinite" />
      </rect>
      {/* Orbiting fragments */}
      <g style={{ animation: "rotate 30s linear infinite", transformOrigin: "800px 450px" }}>
        {[0, 60, 120, 180, 240, 300].map((angle, i) => {
          const rad = (angle * Math.PI) / 180;
          const r = 280;
          const x = 800 + Math.cos(rad) * r;
          const y = 450 + Math.sin(rad) * r * 0.7;
          return (
            <g key={i} transform={`translate(${x}, ${y}) rotate(${angle})`}>
              <polygon points="0,-10 8,0 0,10 -8,0" fill="#B06EFF" opacity="0.5" />
            </g>
          );
        })}
      </g>
      {/* Floor */}
      <rect y="710" width="1600" height="190" fill="#020108" />
      <rect y="710" width="1600" height="2" fill="#B06EFF" opacity="0.5" />
      {/* Floor reflection of mirror line */}
      <ellipse cx="800" cy="712" rx="200" ry="6" fill="#B06EFF" opacity="0.3" />
      {/* Kael — left side */}
      <g transform="translate(640, 660)">
        <ellipse cx="0" cy="46" rx="20" ry="3" fill="#B06EFF" opacity="0.6" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#0a0510" />
        <rect x="-9" y="5" width="18" height="32" fill="#0c0612" />
        <rect x="-7" y="-12" width="14" height="14" fill="#0c0612" />
        <rect x="14" y="0" width="22" height="3" fill="#2a1f3a" />
        <circle cx="-2" cy="-7" r="1" fill="#B06EFF">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="2s" repeatCount="indefinite" />
        </circle>
        <circle cx="3" cy="-7" r="1" fill="#B06EFF">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="2s" repeatCount="indefinite" />
        </circle>
      </g>
      {/* El Verdadero Soldado — right side, mirror image */}
      <g transform="translate(960, 660) scale(-1, 1)">
        <ellipse cx="0" cy="46" rx="20" ry="3" fill="#ff4c4c" opacity="0.6" />
        <path d="M-12,5 L-16,42 L16,42 L12,5 Z" fill="#0a0510" />
        <rect x="-9" y="5" width="18" height="32" fill="#0c0612" />
        <rect x="-7" y="-12" width="14" height="14" fill="#0c0612" />
        <rect x="14" y="0" width="22" height="3" fill="#3a1a1a" />
        <circle cx="-2" cy="-7" r="1" fill="#ff4c4c">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="2s" repeatCount="indefinite" />
        </circle>
        <circle cx="3" cy="-7" r="1" fill="#ff4c4c">
          <animate attributeName="opacity" values="0.6;1;0.6" dur="2s" repeatCount="indefinite" />
        </circle>
      </g>
      {/* Light beam between them */}
      <line x1="660" y1="660" x2="940" y2="660" stroke="#B06EFF" strokeWidth="1" opacity="0.5" strokeDasharray="4,8">
        <animate attributeName="stroke-dashoffset" values="0;-24" dur="2s" repeatCount="indefinite" />
      </line>
      {/* Floating broken fragments scattered */}
      {[
        { x: 200, y: 300, r: 25 },
        { x: 1400, y: 350, r: -20 },
        { x: 300, y: 600, r: 45 },
        { x: 1350, y: 580, r: -30 },
        { x: 400, y: 200, r: 10 },
        { x: 1200, y: 200, r: -15 },
      ].map((s, i) => (
        <g key={i} transform={`translate(${s.x},${s.y}) rotate(${s.r})`} style={{ animation: `float ${4 + i * 0.5}s ease-in-out ${i * 0.3}s infinite` }}>
          <polygon points="0,-8 6,0 0,8 -6,0" fill="#B06EFF" opacity="0.4" stroke="#fff" strokeOpacity="0.3" strokeWidth="0.5" />
        </g>
      ))}
      {/* Vignette */}
      <radialGradient id="finalVignette" cx="50%" cy="50%" r="60%">
        <stop offset="40%" stopColor="#000" stopOpacity="0" />
        <stop offset="100%" stopColor="#000" stopOpacity="0.7" />
      </radialGradient>
      <rect width="1600" height="900" fill="url(#finalVignette)" />
    </svg>
  );
}

// ═══════════════════════════════════════════════════════════════
// WORLDS CONFIG
// ═══════════════════════════════════════════════════════════════
const WORLDS = [
  { id: "ira", name: "Ira", label: "I · FORTALEZA VOLCÁNICA", boss: "Centinela Ardiente", accent: "#FF6B35", accentRgb: "255, 107, 53", scene: IraScene, quote: "la rabia consume al que la niega" },
  { id: "tristeza", name: "Tristeza", label: "II · CIUDAD SUMERGIDA", boss: "Doncella del Lamento", accent: "#00D4FF", accentRgb: "0, 212, 255", scene: TristezaScene, quote: "el dolor también es parte del yo" },
  { id: "miedo", name: "Miedo", label: "III · BOSQUE TENEBROSO", boss: "Espectro del Pavor", accent: "#7FE0B0", accentRgb: "127, 224, 176", scene: MiedoScene, quote: "lo que ocultas en la sombra crece" },
  { id: "culpa", name: "Culpa", label: "IV · CAMPOS ARRASADOS", boss: "Juez Silente", accent: "#C9A567", accentRgb: "201, 165, 103", scene: CulpaScene, quote: "el peso del ayer no define el mañana" },
  { id: "esperanza", name: "Esperanza", label: "V · JARDÍN ETÉREO", boss: "—  antesala", accent: "#FFE4A0", accentRgb: "255, 228, 160", scene: EsperanzaScene, quote: "respira. el camino continúa" },
  { id: "final", name: "Origen", label: "VI · ESPEJO DEL ORIGEN", boss: "El Verdadero Soldado", accent: "#B06EFF", accentRgb: "176, 110, 255", scene: FinalScene, quote: "no destruir. integrar." },
];

// ═══════════════════════════════════════════════════════════════
// MAIN APP
// ═══════════════════════════════════════════════════════════════
export default function App() {
  const [worldIdx, setWorldIdx] = useState(0);
  const [selected, setSelected] = useState(0);
  const [menuKey, setMenuKey] = useState(0);
  const options = ["Continuar", "Reiniciar", "Menú principal"];
  const world = WORLDS[worldIdx];
  const SceneComponent = world.scene;

  useEffect(() => {
    const handleKey = (e) => {
      if (e.key === "ArrowDown" || e.key === "s") { e.preventDefault(); setSelected((s) => (s + 1) % options.length); }
      else if (e.key === "ArrowUp" || e.key === "w") { e.preventDefault(); setSelected((s) => (s - 1 + options.length) % options.length); }
      else if (e.key === "ArrowRight" || e.key === "d") { e.preventDefault(); setWorldIdx((w) => (w + 1) % WORLDS.length); }
      else if (e.key === "ArrowLeft" || e.key === "a") { e.preventDefault(); setWorldIdx((w) => (w - 1 + WORLDS.length) % WORLDS.length); }
    };
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, []);

  // Re-trigger entrance animation when changing worlds
  useEffect(() => { setMenuKey((k) => k + 1); }, [worldIdx]);

  return (
    <div style={{
      position: "relative",
      width: "100%",
      height: "100vh",
      overflow: "hidden",
      background: "#000",
      fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, system-ui, sans-serif",
      letterSpacing: "0.02em",
    }}>
      <style>{`
        @keyframes overlayIn { from { opacity: 0; } to { opacity: 1; } }
        @keyframes slideDown { from { opacity: 0; transform: translateY(-10px); } to { opacity: 1; transform: translateY(0); } }
        @keyframes slideUp { from { opacity: 0; transform: translateY(15px); } to { opacity: 1; transform: translateY(0); } }
        @keyframes float { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(-6px); } }
        @keyframes ember { 0% { transform: translateY(0); opacity: 0; } 20% { opacity: 0.7; } 100% { transform: translateY(-400px); opacity: 0; } }
        @keyframes bubble { 0% { transform: translateY(0); opacity: 0; } 20% { opacity: 0.6; } 100% { transform: translateY(-500px); opacity: 0; } }
        @keyframes rain { 0% { transform: translateY(0); opacity: 0; } 20% { opacity: 0.5; } 100% { transform: translateY(900px); opacity: 0; } }
        @keyframes ash { 0% { transform: translateY(0); opacity: 0; } 20% { opacity: 0.7; } 100% { transform: translateY(900px); opacity: 0; } }
        @keyframes firefly { 0% { transform: translateY(0); opacity: 0; } 20% { opacity: 1; } 100% { transform: translateY(-500px); opacity: 0; } }
        @keyframes fogDrift { 0%, 100% { transform: translateX(0); } 50% { transform: translateX(30px); } }
        @keyframes blink { 0%, 90%, 100% { opacity: 0.7; } 92%, 95% { opacity: 0; } }
        @keyframes rotate { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
        @keyframes widthExpand { from { width: 0; } to { width: 40px; } }
        .option-row { transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
        .tab-btn { transition: all 0.2s ease; }
        .tab-btn:hover { opacity: 1 !important; }
      `}</style>

      {/* GAME SCENE */}
      <SceneComponent key={`scene-${worldIdx}`} />

      {/* PAUSE OVERLAY */}
      <div key={menuKey} style={{
        position: "absolute", inset: 0,
        background: "rgba(6, 6, 14, 0.55)",
        backdropFilter: "blur(10px) saturate(1.15)",
        WebkitBackdropFilter: "blur(10px) saturate(1.15)",
        animation: "overlayIn 0.5s ease both",
      }}>
        {/* Vignette */}
        <div style={{
          position: "absolute", inset: 0,
          background: "radial-gradient(ellipse at center, transparent 35%, rgba(0,0,0,0.55) 100%)",
          pointerEvents: "none",
        }} />

        {/* World tabs */}
        <div style={{
          position: "absolute", top: "24px", left: "50%", transform: "translateX(-50%)",
          display: "flex", gap: "4px",
          background: "rgba(0,0,0,0.4)",
          border: "1px solid rgba(255,255,255,0.08)",
          padding: "6px",
          backdropFilter: "blur(6px)",
          zIndex: 10,
        }}>
          {WORLDS.map((w, i) => (
            <button
              key={w.id}
              className="tab-btn"
              onClick={() => setWorldIdx(i)}
              style={{
                background: worldIdx === i ? `rgba(${w.accentRgb}, 0.15)` : "transparent",
                border: "none",
                borderBottom: worldIdx === i ? `1px solid ${w.accent}` : "1px solid transparent",
                color: worldIdx === i ? w.accent : "rgba(255,255,255,0.5)",
                padding: "6px 14px",
                fontSize: "10px",
                letterSpacing: "2px",
                textTransform: "uppercase",
                cursor: "pointer",
                fontFamily: "inherit",
                opacity: worldIdx === i ? 1 : 0.6,
              }}
            >
              {w.name}
            </button>
          ))}
        </div>

        {/* MENU CONTENT */}
        <div style={{
          position: "absolute",
          top: "50%", left: "50%",
          transform: "translate(-50%, -50%)",
          textAlign: "center",
          color: "#fff",
          minWidth: "340px",
          userSelect: "none",
        }}>
          {/* Pause icon */}
          <div style={{
            display: "flex", justifyContent: "center",
            marginBottom: "20px",
            animation: "slideDown 0.6s ease 0.15s both",
          }}>
            <svg width="22" height="26" viewBox="0 0 22 26">
              <rect x="3" y="2" width="5" height="22" rx="1" fill="rgba(255,255,255,0.95)" />
              <rect x="14" y="2" width="5" height="22" rx="1" fill="rgba(255,255,255,0.95)" />
            </svg>
          </div>

          {/* PAUSED */}
          <div style={{
            fontSize: "12px", letterSpacing: "10px",
            fontWeight: 300, color: "rgba(255,255,255,0.75)",
            marginBottom: "6px", textIndent: "10px",
            animation: "slideDown 0.6s ease 0.25s both",
          }}>PAUSED</div>

          {/* World label */}
          <div style={{
            fontSize: "10px", letterSpacing: "5px",
            fontWeight: 300, color: world.accent,
            marginBottom: "4px", textIndent: "5px",
            textShadow: `0 0 12px rgba(${world.accentRgb}, 0.5)`,
            animation: "slideDown 0.6s ease 0.3s both",
          }}>{world.label}</div>

          {/* Boss line */}
          <div style={{
            fontSize: "9px", letterSpacing: "3px",
            color: "rgba(255,255,255,0.35)",
            marginBottom: "10px", textTransform: "uppercase",
            animation: "slideDown 0.6s ease 0.32s both",
          }}>jefe: {world.boss}</div>

          {/* Divider */}
          <div style={{
            width: "40px", height: "1px",
            background: `linear-gradient(90deg, transparent, ${world.accent}, transparent)`,
            boxShadow: `0 0 8px rgba(${world.accentRgb}, 0.5)`,
            margin: "0 auto 36px",
            animation: "widthExpand 0.6s ease 0.4s both",
          }} />

          {/* OPTIONS */}
          <div style={{ display: "flex", flexDirection: "column", gap: "22px", alignItems: "center" }}>
            {options.map((option, i) => (
              <div
                key={`${worldIdx}-${option}`}
                className="option-row"
                onClick={() => setSelected(i)}
                onMouseEnter={() => setSelected(i)}
                style={{
                  position: "relative",
                  padding: "6px 36px",
                  fontSize: "22px",
                  fontWeight: 300,
                  letterSpacing: "3px",
                  color: selected === i ? "#fff" : "rgba(255,255,255,0.45)",
                  textShadow: selected === i ? `0 0 24px rgba(${world.accentRgb}, 0.7), 0 0 4px rgba(${world.accentRgb}, 0.5)` : "none",
                  cursor: "pointer",
                  animation: `slideUp 0.5s ease ${0.5 + i * 0.08}s both`,
                  transform: selected === i ? "scale(1.04)" : "scale(1)",
                }}
              >
                <div style={{
                  position: "absolute", left: 0, top: "50%",
                  transform: "translateY(-50%)",
                  width: selected === i ? "18px" : "0px",
                  height: "1.5px",
                  background: world.accent,
                  boxShadow: `0 0 8px ${world.accent}, 0 0 14px rgba(${world.accentRgb}, 0.6)`,
                  transition: "width 0.3s cubic-bezier(0.16, 1, 0.3, 1)",
                }} />
                <div style={{
                  position: "absolute", right: 0, top: "50%",
                  transform: "translateY(-50%)",
                  width: selected === i ? "18px" : "0px",
                  height: "1.5px",
                  background: world.accent,
                  boxShadow: `0 0 8px ${world.accent}, 0 0 14px rgba(${world.accentRgb}, 0.6)`,
                  transition: "width 0.3s cubic-bezier(0.16, 1, 0.3, 1)",
                }} />
                {option}
              </div>
            ))}
          </div>

          {/* Quote (world flavor) */}
          <div style={{
            marginTop: "44px",
            fontSize: "11px", letterSpacing: "2px",
            color: `rgba(${world.accentRgb}, 0.6)`,
            fontStyle: "italic",
            animation: "slideUp 0.5s ease 0.75s both",
          }}>"{world.quote}"</div>

          {/* Helper text */}
          <div style={{
            marginTop: "32px",
            fontSize: "10px", letterSpacing: "3px",
            color: "rgba(255,255,255,0.35)",
            textTransform: "uppercase",
            animation: "slideUp 0.5s ease 0.85s both",
          }}>
            <span style={{ color: `rgba(${world.accentRgb}, 0.75)` }}>↑↓</span>
            <span style={{ margin: "0 6px" }}>navegar</span>
            <span style={{ color: "rgba(255,255,255,0.2)", margin: "0 6px" }}>·</span>
            <span style={{ color: `rgba(${world.accentRgb}, 0.75)` }}>ENTER</span>
            <span style={{ margin: "0 6px" }}>seleccionar</span>
            <span style={{ color: "rgba(255,255,255,0.2)", margin: "0 6px" }}>·</span>
            <span style={{ color: `rgba(${world.accentRgb}, 0.75)` }}>ESC</span>
            <span style={{ margin: "0 0 0 6px" }}>continuar</span>
          </div>
        </div>

        {/* Corner brackets */}
        {[
          { top: "84px", left: "40px", borders: { borderTop: `1.5px solid rgba(${world.accentRgb}, 0.6)`, borderLeft: `1.5px solid rgba(${world.accentRgb}, 0.6)` } },
          { top: "84px", right: "40px", borders: { borderTop: `1.5px solid rgba(${world.accentRgb}, 0.6)`, borderRight: `1.5px solid rgba(${world.accentRgb}, 0.6)` } },
          { bottom: "40px", left: "40px", borders: { borderBottom: `1.5px solid rgba(${world.accentRgb}, 0.6)`, borderLeft: `1.5px solid rgba(${world.accentRgb}, 0.6)` } },
          { bottom: "40px", right: "40px", borders: { borderBottom: `1.5px solid rgba(${world.accentRgb}, 0.6)`, borderRight: `1.5px solid rgba(${world.accentRgb}, 0.6)` } },
        ].map((c, i) => (
          <div key={i} style={{
            position: "absolute", width: "28px", height: "28px",
            ...c, ...c.borders,
            animation: `${i < 2 ? "slideDown" : "slideUp"} 0.6s ease 0.5s both`,
          }} />
        ))}

        {/* World navigation hint */}
        <div style={{
          position: "absolute", bottom: "20px", left: "50%",
          transform: "translateX(-50%)",
          fontSize: "9px", letterSpacing: "3px",
          color: "rgba(255,255,255,0.3)",
          textTransform: "uppercase",
        }}>
          <span style={{ color: `rgba(${world.accentRgb}, 0.6)` }}>← →</span>
          <span style={{ margin: "0 6px" }}>cambiar mundo</span>
        </div>
      </div>
    </div>
  );
}
