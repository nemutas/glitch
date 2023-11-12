import * as THREE from 'three'
import { three } from './core/Three'
import vertexShader from './shader/screen.vs'
import fragmentShader from './shader/screen.fs'
import { gui } from './Gui'

export class Canvas {
  private screen!: THREE.Mesh<THREE.PlaneGeometry, THREE.RawShaderMaterial>

  constructor(canvas: HTMLCanvasElement) {
    this.load().then((texture) => {
      this.init(canvas)
      this.screen = this.createScreen(texture)
      this.setGui()
      this.addEvents()
      three.animation(this.anime)
    })
  }

  private async load() {
    const loader = new THREE.TextureLoader()
    loader.setPath(import.meta.env.BASE_URL)
    const texture = await loader.loadAsync('image.jpg')
    texture.wrapS = THREE.MirroredRepeatWrapping
    texture.wrapT = THREE.RepeatWrapping
    texture.userData.aspect = texture.source.data.width / texture.source.data.height
    return texture
  }

  private init(canvas: HTMLCanvasElement) {
    three.setup(canvas)
  }

  private createScreen(texture: THREE.Texture) {
    const geometry = new THREE.PlaneGeometry(2, 2)
    const material = new THREE.RawShaderMaterial({
      uniforms: {
        tImage: { value: texture },
        uCoveredScale: { value: three.coveredScale(texture.userData.aspect) },
        uTime: { value: 0 },
        uSaturation: { value: 1 },
        uFlag: {
          value: {
            waveform_wrap: false,
            deform_line: false,
            rgb_block_noise: true,
            tri_tone: true,
            chromatic_aberration: true,
            ghost: true,
            scan_line: true,
            scan_line_up: true,
            grain_noise: true,
          },
        },
      },
      vertexShader,
      fragmentShader,
    })

    const mesh = new THREE.Mesh(geometry, material)
    three.scene.add(mesh)
    return mesh
  }

  private setGui() {
    const frag = this.screen.material.uniforms.uFlag.value
    for (let name of Object.keys(frag)) {
      gui.add(frag, name)
    }

    gui.add(this.screen.material.uniforms.uSaturation, 'value', 0.5, 2.0, 0.01).name('saturation')
  }

  private addEvents() {
    three.addEventListener('resize', () => {
      const uniforms = this.screen.material.uniforms
      uniforms.uCoveredScale.value = three.coveredScale(uniforms.tImage.value.userData.aspect)
    })
  }

  private anime = () => {
    this.screen.material.uniforms.uTime.value += three.time.delta
    three.render()
  }

  dispose() {
    three.dispose()
  }
}
