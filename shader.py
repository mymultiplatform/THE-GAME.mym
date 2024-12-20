// Created by sebastien durand - 08/2016
//-------------------------------------------------------------------------------------
// Based on "Dusty nebula 4" by Duke (https://www.shadertoy.com/view/MsVXWW) 
// Sliders from IcePrimitives by Bers (https://www.shadertoy.com/view/MscXzn)
// License: Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
//-------------------------------------------------------------------------------------


#define R(p, a) p=cos(a)*p+sin(a)*vec2(p.y, -p.x)
#define pi 3.14159265


const vec4 
    colCenter = vec4(1.2, 1.5,1.5,.25),
	colEdge = vec4(.1,.1,.2,.5),
	colEdge2 = vec4(.7,.54,.3,.23),
    colEdge3 = vec4(.6,1.,1.3,.25);

const float time = 10.;
vec4 sliderVal;

vec2 min2(vec2 a, vec2 b) {
    return a.x<b.x ? a  : b;
} 

float hash( const in vec3 p ) {
	float h = dot(p,vec3(127.1,311.7,758.5453123));	
    return fract(sin(h)*43758.5453123);
}

// [iq] https://www.shadertoy.com/view/4sfGzS
float noiseText(in vec3 x) {
    vec3 p = floor(x), f = fract(x);
	f = f*f*(3.-f-f);
	vec2 uv = (p.xy+vec2(37.,17.)*p.z) + f.xy,
	     rg = textureLod( iChannel0, (uv+.5)/256., -100.).yx;
	return mix(rg.x, rg.y, f.z);
}

// ratio: ratio of hight/low frequencies
float fbmdust(in vec3 p, in float ratio) {
    return mix(noiseText(p*3.), noiseText(p*20.), ratio);
}

vec2 spiralArm(in vec3 p, in float thickness, in float blurAmout, in float blurStyle) {
    float dephase = 2.2, loop = 4.;
    float a = atan(p.x,p.z),  // angle     
		  r = length(p.xz), lr = log(r), // distance to center
    	  th = (.1-.25*r), // thickness according to distance
    	  d = fract(.5*(a-lr*loop)/pi); //apply rotation and scaling.
    d = (.5/dephase - abs(d-.5))*2.*pi*r;
  	d *= (1.-lr)/thickness;  // space fct of distance
    // Perturb distance field
    float radialBlur = blurAmout*fbmdust(vec3(r*4.,10.*d,10.-5.*p.y),blurStyle);
    return vec2(sqrt(d*d+10.*p.y*p.y/thickness)-th*r*.2-radialBlur);
}

vec2 dfGalaxy(in vec3 p, in float thickness, in float blurAmout, in float blurStyle) {
	return min2(spiralArm(p,                  thickness, blurAmout, blurStyle),
    			spiralArm(vec3(p.z,p.y,-p.x), thickness, blurAmout, blurStyle));  
}

vec2 map(in vec3 p) {
	R(p.xz, iMouse.x*.008*pi+iTime*.3);
    return dfGalaxy(p, clamp(10.*sliderVal.x,.9,10.), sliderVal.y, sliderVal.z);
}

//--------------------------------------------------------------

// assign color to the media
vec4 computeColor(in vec3 p, in float density, in float radius, in float id) {
	// color based on density alone, gives impression of occlusion within
	// the media
	vec4 result = mix( vec4(1.,.9,.8,1.), vec4(.4,.15,.1,1.), density );
	// color added to the media
	result *= mix( colCenter,
                  mix(colEdge2, 
                      mix(colEdge, colEdge3, step(.08,id)), step(-.05,id)),
                  smoothstep(.2,.8,radius) );
	return result;
}

// - Ray / Shapes Intersection -----------------------
bool sBox( in vec3 ro, in vec3 rd, in vec3 rad, out float tN, out float tF)  {
    vec3 m = 1./rd, n = m*ro,
    	k = abs(m)*rad,
        t1 = -n - k, t2 = -n + k;
	tN = max( max( t1.x, t1.y ), t1.z );
	tF = min( min( t2.x, t2.y ), t2.z );
	return !(tN > tF || tF < 0.);
}

bool sSphere(in vec3 ro, in vec3 rd, in float r, out float tN, out float tF) {
	float b = dot(rd, ro), d = b*b - dot(ro, ro) + r;
	if (d < 0.) return false;
	tN = -b - sqrt(d);
	tF = -tN-b-b;
	return tF > 0.;
}

// ---------------------------------------------------
// Bers : https://www.shadertoy.com/view/MscXzn
vec4 processSliders(in vec2 uv) {
    sliderVal = texture(iChannel2,vec2(0));
    if(length(uv.xy)>1.) {
    	return texture(iChannel2,uv.xy/iResolution.xy);
    }
    return vec4(0);
}

// ---------------------------------------------------
// Based on "Dusty nebula 4" by Duke (https://www.shadertoy.com/view/MsVXWW) 
void mainImage(out vec4 fragColor, in vec2 fragCoord ) {  
    vec4 cSlider = processSliders(fragCoord);

   // camera	   
    float a = sliderVal.w*pi;
    vec3 ro = vec3(0., 2.*cos(a), -4.5*sin(a)),
         ta = vec3(-.2,-.3,0);

    // camera tx
    vec3 cw = normalize( ta-ro ),
     	 cp = vec3( 0., 1., 0. ),
     	 cu = normalize( cross(cw,cp) ),
     	 cv = normalize( cross(cu,cw) );
    vec2 q = (fragCoord.xy)/iResolution.xy,
     	 p = -1.+2.*q;
    p.x *= iResolution.x/iResolution.y;
    
    vec3 rd = normalize( p.x*cu + p.y*cv + 2.5*cw );
      
	// ld, td: local, total density 
	// w: weighting factor
	float ld=0., td=0., w=0.;

	// t: length of the ray
	// d: distance function
	float d=1., t=0.;
    
    const float h = 0.1;
   
	vec4 sum = vec4(0);
   
    float min_dist=0.,  max_dist=0.,
          min_dist2=0., max_dist2=0.;
    
    if(sSphere(ro, rd, 4., min_dist, max_dist)) {
        if (sBox(ro, rd, vec3(4.,1.8,4.), min_dist2, max_dist2)) {
        	min_dist = max(.1,max(min_dist, min_dist2));
            max_dist = min(max_dist, max_dist2);
            
            t = min_dist*step(t,min_dist) + .1*hash(rd+iTime);
			
            
            // raymarch loop
            vec4 col;        
            for (int i=min(iFrame,0); i<100; i++) {	 
                vec3 pos = ro + t*rd;

                // Loop break conditions.
                if(td > .9 || sum.a > .99 || t > max_dist) break;

                // evaluate distance function
                vec2 res = map(pos);
                d = max(res.x,.01); 
				
                // point light calculations
                vec3 ldst = pos;
                ldst.y*=1.6;
                vec3 ldst2 = pos;
                ldst2.y*=3.6;
                float lDist = max(length(ldst),.1), //max(length(ldst), 0.001);
				      lDist2 = max(length(ldst2),.1);
                // star in center
                vec3 lightColor = (1.-smoothstep(3.,4.5,lDist*lDist))*
                    mix(.015*vec3(1.,.5,.25)/(lDist*lDist),
                        .02*vec3(.5,.7,1.)/(lDist2*lDist2), 
                        smoothstep(.1,2.,lDist*lDist));
                sum.rgb += lightColor; //.015*lightColor/(lDist*lDist); // star itself and bloom around the light
                sum.a += .003/(lDist*lDist);;

                if (d<h) {
                    // compute local density 
                    ld = h - d;
                    // compute weighting factor 
                    w = (1. - td) * ld;
                    // accumulate density
                    td += w + 1./60.;
                    // get color of object (with transparencies)
                    col = computeColor(pos, td,lDist*2., res.y);
                    col.a *= td;
                    // colour by alpha
                    col.rgb *= col.a;
                    // alpha blend in contribution
                    sum += col*(1.0 - sum.a);  
                }
  
                //float pitch = t/iResolution.x;
                //float dt = max(d * 0.25, .005); //pitch);
                // trying to optimize step size near the camera and near the light source
                t += max(d * .15 * max(min(length(ldst), length(ro)),1.0), 0.005);
                td += .1/70.;
                //t += dt;
            }
            // simple scattering
            sum *= 1. / exp( ld * .2 )*.8 ;  
            sum = clamp( sum, 0., 1. );
    	}
    }
        
	// Background color
    sum.rgb += vec3(clamp(2.*cos(.5*iTime),0.,.4))*(1. - sum.a)*pow(16.0*q.x*q.y*(1.-q.x)*(1.-q.y),.3);  
 
    //Apply slider overlay
    fragColor = vec4(mix(sum.xyz,cSlider.rgb,cSlider.a), 1.);

}
