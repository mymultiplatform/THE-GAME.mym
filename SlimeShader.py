// "Quick Slime 3D" by dr2 - 2020
// License: Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License

float Hashff (float p);
vec4 Loadv4 (int idVar);
void Savev4 (int idVar, vec4 val, inout vec4 fCol, vec2 fCoord);

const int nBall = 36;

vec3 rLead;
float tCur, nStep, obSpc, obSz;

#define VAR_ZERO min (iFrame, 0)

void Step (int mId, out vec3 rm, out vec3 vm, out vec3 wm, out float sz)
{
  vec4 p;
  vec3 rmN, vmN, wmN, dr, dv, drw, am, wam;
  float fOvlap, fricN, fricT, fricS, fricSW, fDamp, fAttr, grav, rSep, szN, szAv,
     fc, ft, drv, dt;
  fOvlap = 1000.;
  fricN = 2.;
  fricS = 0.05;
  fricSW = 10.;
  fricT = 0.5;
  fAttr = 0.05;
  fDamp = 0.01;
  grav = 5.;
  p = Loadv4 (3 + 3 * mId);
  rm = p.xyz;
  sz = p.w;
  vm = Loadv4 (3 + 3 * mId + 1).xyz;
  wm = Loadv4 (3 + 3 * mId + 2).xyz;
  am = vec3 (0.);
  wam = vec3 (0.);
  for (int n = VAR_ZERO; n < nBall; n ++) {
    p = Loadv4 (3 + 3 * n);
    rmN = p.xyz;
    szN = p.w;
    dr = rm - rmN;
    rSep = length (dr);
    szAv = 0.5 * (sz + szN);
    if (n != mId && rSep < szAv) {
      fc = fOvlap * (szAv / rSep - 1.);
      vmN = Loadv4 (3 + 3 * n + 1).xyz;
      wmN = Loadv4 (3 + 3 * n + 2).xyz;
      dv = vm - vmN;
      drv = dot (dr, dv) / (rSep * rSep);
      fc = max (fc - fricN * drv, 0.);
      am += fc * dr;
      dv -= drv * dr + cross ((sz * wm + szN * wmN) / (sz + szN), dr);
      ft = min (fricT, fricS * abs (fc) * rSep / max (0.001, length (dv)));
      am -= ft * dv;
      wam += (ft / rSep) * cross (dr, dv);
    }
    am += fAttr * (rmN - rm);
  }
  szAv = 0.5 * (sz + 1.);
  dr = vec3 (0., rm.y, 0.);
  rSep = abs (dr.y);
  if (rSep < szAv) {
    fc = fOvlap * (szAv / rSep - 1.);
    dv = vm;
    drv = dot (dr, dv) / (rSep * rSep);
    fc = max (fc - fricN * drv, 0.);
    am += fc * dr;
    dv -= drv * dr + cross (wm, dr);
    ft = min (fricT, fricSW * abs (fc) * rSep / max (0.001, length (dv)));
    am -= ft * dv;
    wam += (ft / rSep) * cross (dr, dv);
  }
  szAv = 0.5 * (sz + obSz);
  dr = rm;
  dr.xz -= obSpc * floor ((rm.xz + 0.5 * obSpc) / obSpc);
  rSep = length (dr);
  if (rSep < szAv) {
    fc = fOvlap * (szAv / rSep - 1.);
    dv = vm;
    drv = dot (dr, dv) / (rSep * rSep);
    fc = max (fc - fricN * drv, 0.);
    am += fc * dr;
    dv -= drv * dr + cross (wm, dr);
    ft = min (fricT, fricSW * abs (fc) * rSep / max (0.001, length (dv)));
    am -= ft * dv;
    wam += (ft / rSep) * cross (dr, dv);
  }
  am += fAttr * (rLead - rm);
  am.y -= grav;
  am -= fDamp * vm;
  dt = 0.02;
  vm += dt * am;
  rm += dt * vm;
  wm += dt * wam / (0.1 * sz);
}

void Init (int mId, out vec3 rm, out vec3 vm, out vec3 wm, out float sz)
{
  float mIdf, nbEdge;
  nbEdge = floor (sqrt (float (nBall)) + 0.01);
  mIdf = float (mId);
  rm = vec3 (floor (vec2 (mod (mIdf, nbEdge), mIdf / nbEdge)) - 0.5 * (nbEdge - 1.), 3.).xzy;
  vm = 2. * normalize (vec3 (Hashff (mIdf), Hashff (mIdf + tCur + 0.3),
     Hashff (mIdf + 0.6)) - 0.5);
  wm = vec3 (0.);
  sz = 1. - 0.1 * Hashff (mIdf + 0.1);
}

const float txRow = 128.;

void mainImage (out vec4 fragColor, in vec2 fragCoord)
{
  vec4 stDat;
  vec3 rm, vm, wm, rMid;
  vec2 iFrag;
  float sz;
  int mId, pxId;
  bool doInit;
  iFrag = floor (fragCoord);
  pxId = int (iFrag.x + txRow * iFrag.y);
  if (iFrag.x >= txRow || pxId >= 3 * nBall + 3) discard;
  tCur = iTime;
  if (pxId >= 3) mId = (pxId - 3) / 3;
  else mId = -1;
  doInit = false;
  if (iFrame <= 5) {
    obSpc = 8.;
    obSz = 3.;
    doInit = true;
  } else {
    stDat = Loadv4 (0);
    nStep = stDat.x;
    obSpc = stDat.y;
    obSz = stDat.z;
    rLead = Loadv4 (1).xyz;
  }
  if (doInit) {
    nStep = 0.;
    rLead = vec3 (0., 0., 0.);
    if (mId >= 0) Init (mId, rm, vm, wm, sz);
  } else {
    ++ nStep;
    rLead += 0.05 * vec3 (0.9, 0., 1.);
    if (mId >= 0) Step (mId, rm, vm, wm, sz);
  }
  if (pxId == 2) {
    rMid = vec3 (0.);
    for (int n = VAR_ZERO; n < nBall; n ++) rMid += Loadv4 (3 + 3 * n).xyz;
    rMid /= float (nBall);
  }
  if      (pxId == 0) stDat = vec4 (nStep, obSpc, obSz, 0.);
  else if (pxId == 1) stDat = vec4 (rLead, 0.);
  else if (pxId == 2) stDat = vec4 (rMid, 0.);
  else if (pxId == 3 + 3 * mId) stDat = vec4 (rm, sz);
  else if (pxId == 3 + 3 * mId + 1) stDat = vec4 (vm, 0.);
  else if (pxId == 3 + 3 * mId + 2) stDat = vec4 (wm, 0.);
  Savev4 (pxId, stDat, fragColor, fragCoord);
}

const float cHashM = 43758.54;

float Hashff (float p)
{
  return fract (sin (p) * cHashM);
}

#define txBuf iChannel0
#define txSize iChannelResolution[0].xy

vec4 Loadv4 (int idVar)
{
  float fi;
  fi = float (idVar);
  return texture (txBuf, (vec2 (mod (fi, txRow), floor (fi / txRow)) + 0.5) /
     txSize);
}

void Savev4 (int idVar, vec4 val, inout vec4 fCol, vec2 fCoord)
{
  vec2 d;
  float fi;
  fi = float (idVar);
  d = abs (fCoord - vec2 (mod (fi, txRow), floor (fi / txRow)) - 0.5);
  if (max (d.x, d.y) < 0.5) fCol = val;
}
