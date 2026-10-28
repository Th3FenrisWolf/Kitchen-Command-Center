using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public static class DeterministicProbes
{
    public const string Script = """
        () => {
          const has = (style, property, none) => {
            const value = style[property]
            return !!value && value !== none
          }
          const filtered = (style) => has(style, 'filter', 'none')
          const blended = (style) => has(style, 'mixBlendMode', 'normal')
          const backdrop = (style) => has(style, 'backdropFilter', 'none') || has(style, 'webkitBackdropFilter', 'none')
          const clipped = (style) => has(style, 'clipPath', 'none')
          const counts = { filtered_elements: 0, clip_path_elements: 0, blend_elements: 0, backdrop_elements: 0, filtered_area_px2: 0 }
          const styles = new Map()
          const styleOf = (element) => {
            let style = styles.get(element)
            if (!style) {
              style = getComputedStyle(element)
              styles.set(element, style)
            }
            return style
          }
          for (const element of document.querySelectorAll('*')) {
            const style = styleOf(element)
            if (filtered(style)) {
              counts.filtered_elements++
              const rect = element.getBoundingClientRect()
              counts.filtered_area_px2 += rect.width * rect.height
            }
            if (clipped(style)) counts.clip_path_elements++
            if (blended(style)) counts.blend_elements++
            if (backdrop(style)) counts.backdrop_elements++
            for (const pseudo of ['::before', '::after']) {
              const style = getComputedStyle(element, pseudo)
              if (filtered(style)) counts.filtered_elements++
              if (clipped(style)) counts.clip_path_elements++
              if (blended(style)) counts.blend_elements++
              if (backdrop(style)) counts.backdrop_elements++
            }
          }
          const underFilter = (element) => {
            for (let parent = element.parentElement; parent; parent = parent.parentElement) {
              if (filtered(styleOf(parent))) return true
            }
            return false
          }
          counts.animations_running = 0
          counts.animated_filtered = 0
          counts.animated_under_filter = 0
          for (const animation of document.getAnimations()) {
            if (animation.playState !== 'running') continue
            const target = animation.effect && animation.effect.target
            if (!target) continue
            counts.animations_running++
            const style = styleOf(target)
            if (filtered(style) || blended(style)) counts.animated_filtered++
            if (underFilter(target)) counts.animated_under_filter++
          }
          counts.filtered_area_px2 = Math.round(counts.filtered_area_px2)
          return counts
        }
        """;

    public static Task<Dictionary<string, double>> ReadAsync(IPage page) =>
        Collectors.ReadAsync<Dictionary<string, double>>(page, Script);
}
