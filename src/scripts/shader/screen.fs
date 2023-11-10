// reference
// https://youtu.be/zyRCDYmO1VQ?si=FI356kUpqRgePzZe

precision mediump float;

struct Flag {
  bool waveform_wrap;
  bool deform_line;
  bool rgb_block_noise;
  bool chromatic_aberration;
  bool ghost;
  bool scan_line;
  bool scan_line_up;
  bool grain_noise;
  bool tri_tone;
};

uniform sampler2D tImage;
uniform vec2 uCoveredScale;
uniform float uTime;
uniform Flag uFlag;

varying vec2 vUv;

#include './modules/snoise21.glsl'
#include './modules/blend.glsl'

float hash(vec2 p) {
  return fract(1e4 * sin(17.0 * p.x + p.y * 0.1) * (0.1 + abs(sin(p.y * 13.0 + p.x))));
}

void main() {
  vec2 uv = (vUv - 0.5) * uCoveredScale + 0.5;
  vec4 image = texture2D(tImage, uv);
  vec3 color = image.rgb;

  if (uFlag.waveform_wrap) {
    float n = snoise(vec2(uTime * 15.0));
    float s = sin(vUv.y * 1000.0 + sin(vUv.y * 1000.0)) * n * 0.005;
    image = texture2D(tImage, uv + vec2(s, 0.0));
    color = image.rgb;
  }

  if (uFlag.deform_line) {
    float n = snoise(floor((vUv + vec2(0.0, -uTime * 0.1)) * vec2(1.0, 3.0))) * 0.05;
    image = texture2D(tImage, uv + vec2(n, 0.0));
    color = image.rgb;
  }

  // ==========================

  if (uFlag.rgb_block_noise) {
    float time = floor(uTime * 15.0);
    float n1 = snoise(floor(uv * vec2(7.0, 15.0) + time)) * 0.5 + 0.5;
    float n2 = snoise(floor(uv * vec2(3.0, 5.0) + time)) * 0.5 + 0.5;
    float n3 = snoise(floor(uv * vec2(4.0, 4.0) + time)) * 0.5 + 0.5;
    float n = n1 * n2 * n3;
    n = step(0.2, n);

    vec3 aber;
    aber.r = texture2D(tImage, uv - vec2(0.01)).r;
    aber.g = texture2D(tImage, uv).g;
    aber.b = texture2D(tImage, uv + vec2(0.01)).b;
    color = mix(color, aber, n);
  }

  if (uFlag.tri_tone) {
    float gray = dot(color, vec3(0.299, 0.587, 0.114));
    vec3 tone = vec3(0.82, 0.93, 0.99);
    tone = mix(vec3(0.00, 0.56, 1.00), tone, vec3(gray));
    tone = mix(vec3(0.00, 0.19, 0.32), tone, vec3(gray));
    color = tone;
  }

  if (uFlag.chromatic_aberration) {
    float time = floor(uTime * 15.0);
    float scale = 0.01;
    vec2 nr = vec2(snoise(vec2(time * 3.0)), snoise(vec2(time * 5.0))) * scale;
    vec2 ng = vec2(snoise(vec2(time * 4.0)), snoise(vec2(time * 6.0))) * scale;
    vec2 nb = vec2(snoise(vec2(time * 6.0)), snoise(vec2(time * 4.0))) * scale;
    vec3 aber;
    aber.r = texture2D(tImage, uv + nr).r;
    aber.g = texture2D(tImage, uv + ng).g;
    aber.b = texture2D(tImage, uv + nb).b;
    color = blendPinLight(color, aber, 1.0);
  }

  if (uFlag.ghost) {
    float time = floor(uTime * 3.0);
    float r = hash(vec2(time)) - 0.5;
    r *= 0.5;
    r *= step(0.5, hash(vec2(floor(uTime * 10.0))));
    vec3 ghostImage = texture2D(tImage, uv + vec2(0, 1) * r).rgb;
    color = blendOverlay(color, ghostImage, 0.5);
  }

  if (uFlag.scan_line) {
    float scanLine = sin(uv.y * 2000.0) * 0.5 + 0.5;
    scanLine = scanLine * (1.0 - 0.5) + 0.5;
    color = mix(vec3(0), color, scanLine);
  }

  if (uFlag.scan_line_up) {
    float n = snoise(floor((uv + vec2(0.0, -uTime * 0.1)) * vec2(1.0, 7.0))) * 0.5 + 0.5;
    float opacity = floor(fract(uTime * 20.0) * 3.0);
    opacity = opacity * (0.30 - 0.15) + 0.15;
    color = blendOverlay(color, vec3(n), opacity);
  }

  if (uFlag.grain_noise) {
    float n = hash(uv + uTime);
    color = blendOverlay(color, vec3(n), 0.2);
  }

  gl_FragColor = vec4(color, 1.0);
}